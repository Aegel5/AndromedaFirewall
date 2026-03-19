using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

class Config {
	public static Config Inst { get; private set; } = new();
	static readonly string path = ProgramUtils.FindOurPath("AndromedaDnsFirewall.json");

	static ACounter disableSaveRequest = new();

	static public void Init() {

		SaveLoop();

		if (!File.Exists(path)) {
			return;
		}

		using var saveLocker = disableSaveRequest.Take();
		using var stream = File.OpenRead(path);
		var res = JsonSerializer.Deserialize<Config>(stream, Opt); // blocks this thread!
		if (res == null) throw new Exception("Can't deserialize");
		Inst = res;

	}

	async static void SaveLoop() {
		while (!GlobalData.QuitPending) {
			await Task.Delay(5000);
			if (needSave) {
				needSave = false;
				Save();
			}
		}
	}


	static bool needSave = false;
	public static void NeedSave() {
		if (disableSaveRequest.IsTaked) return;
		needSave = true;
	}

	static JsonSerializerOptions Opt =>
		new JsonSerializerOptions {
			//IncludeFields = true,
			IgnoreReadOnlyFields = true,
			IgnoreReadOnlyProperties = true,
			WriteIndented = true,
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip
		};

	static void Save() {
		using var stream = File.Create(path);
		JsonSerializer.Serialize(stream, Inst, Opt); // blocks this thread!
	}

	public ObservableCollection<PublicBlockEntry> PublicBlockLists { get; set; } = new();
	public bool useServers_DOH { get; set; } = true;
	public List<string> DnsResolvers_DOH { get; set; } = [
		"https://1.0.0.1/dns-query",
		"https://1.1.1.1/dns-query",
		"https://8.8.4.4/dns-query",
		"https://8.8.8.8/dns-query"
		];
	public bool useServers_UDP { get; set; } = false;
	public List<string> DnsResolvers_UDP { get; set; } = [
		"default_gateway",
		"1.0.0.1",
		"1.1.1.1",
		"8.8.4.4",
		"8.8.8.8"
		];
	public string ServerAddress { get; set; } = "127.0.0.1:53";

	public bool AddToAutostart {
		get => field;
		set {
			if (value != field) { field = value; NeedSave(); }
		}
	} = false;

	public int IpCacheTimeMinutes { get; set; } = 15;

	[JsonIgnore]
	public WorkMode mode { get; set; } = WorkMode.AllExceptBlockList;
	static public WorkMode Mode => Inst.mode;
}
