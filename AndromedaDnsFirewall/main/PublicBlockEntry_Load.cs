using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

internal partial class PublicBlockEntry {

	QuickHashType[] cache = [];

	static readonly HttpClient httpClient
		= new(new SocketsHttpHandler { ConnectCallback = ConnectCallback }) { Timeout = 20.sec() };

	static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken) {

		// use our own resolver!
		var ip = await DnsResolver.Inst.ResolveNoCache(context.DnsEndPoint.Host);
		var endPoint = new DnsEndPoint(ip.ToString(), context.DnsEndPoint.Port);

		var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
		await socket.ConnectAsync(endPoint, cancellationToken);
		return new NetworkStream(socket, ownsSocket: true);
	}

	public static void RemoveDuplicatesAndResize<T>(ref T[] items) where T : IEquatable<T> {
		if (items == null || items.Length <= 1) return;

		int uniqueIndex = 0;

		// 1. Сдвигаем уникальные элементы в начало (O(n))
		for (int i = 1; i < items.Length; i++) {
			// Используем метод Equals для сравнения обобщенных типов
			if (!items[i].Equals(items[uniqueIndex])) {
				uniqueIndex++;
				items[uniqueIndex] = items[i];
			}
		}

		int uniqueCount = uniqueIndex + 1;

		// 2. Сжимаем массив, если нашли дубликаты
		if (uniqueCount < items.Length) {
			Array.Resize(ref items, uniqueCount);
		}
	}


	void Apply(byte[] cont) { // no await

		List<QuickHashType> temp = new(8192);
		Utf8Utils.Split(cont, [(byte)'\n', (byte)'\r'], x => {
			x = x.Trim((byte)' ');
			if (x.IsEmpty) return;
			if (x[0] == '#') return;
			var iSpace = x.LastIndexOf((byte)' ');
			if (iSpace != -1) {
				x = x.Slice(iSpace+1);
			}
			temp.Add(HashUtils.QuickHash(x));
		});

		cache = temp.ToArray();
		Array.Sort(cache);

		RemoveDuplicatesAndResize(ref cache);

		//var ok = cache.AsEnumerable().Distinct().Count() == cache.Length;

		dtLastLoad = TimePoint.Now;
		Count = cache.Length;
	}

	public async Task LoadFromUrl() {
		try {

			var resp = await httpClient.GetAsync(Url);
			resp.EnsureSuccessStatusCode();
			using var cont = resp.Content;
			var res = await cont.ReadAsByteArrayAsync();
			Apply(res);

		} catch (Exception ex) {
			Log.Err(ex);
		}
	}

	TimePoint dtLastLoad {
		get => field;
		set {
			field = value;
			UpdateH();
		}
	}
	TimePoint stopLoadingUntil;
	bool IsCacheActual => dtLastLoad.DeltToNow.TotalHours <= UpdateHour;
	public bool Inited => dtLastLoad != default;

	void UpdateH() {
		LastUpdated = dtLastLoad == default ? "never" : $"{(int)dtLastLoad.DeltToNow.TotalHours} hours ago";
	}

	void Clear() {
		cache = [];
		Count = 0;
		dtLastLoad = default;
	}
	ACounter reload = new();
	void UpdateLabel() {
	}
	public async Task UpdateReload() {

		try {

			if (reload.IsTaked) return;
			using var loc = reload.Take();

			UpdateH();

			if (!Enabled) {
				Clear();
				return;
			}

			if (stopLoadingUntil > TimePoint.Now)
				return;

			if (IsCacheActual) return;

			for (int i = 0; i < 3; i++) {
				await LoadFromUrl();
				if (IsCacheActual) return;
				if (Inited) break;
			}

			// произошла ошибка

			if (!Inited) {
				// пробуем через 5 секунд
				stopLoadingUntil = TimePoint.Now.Add(10.sec());
			} else {
				// есть старая версия, поэтому задержка больше.
				stopLoadingUntil = TimePoint.Now.Add(10.min());
			}
		} catch (Exception ex) {
			Log.Err(ex);
		}
	}

	public bool IsNeedBlock(string name) {
		if (!Enabled) return false;
		var key = HashUtils.QuickHash(name.ToUtf8());
		return HashUtils.UltraFastSearch(cache, key) >= 0;
		//return cache.BinarySearch(key) >= 0;
	}

}
