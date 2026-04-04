using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AndromedaDnsFirewall;

enum LogType {
	Blocked,
	Allowed,
	BlockedByUserList,
	AllowedByUserList,
	Exception,
	BadRequest,
	BlockedByPublicList,
	//Allow_PublicBlockListNotReady,
	Block_PublicBlockListNotReady,
	Bypass
}


record LogItem {

	public LogItem() {
		dt = DateTime.UtcNow;
	}
	public int packetSize = -1;
	int fromCacheCnt;
	public bool IsSame(LogItem other) {
		return log_type == other.log_type && domain == other.domain;
	}
	public void OverwriteWith(LogItem other) {
		if(other.responseRaw != null) responseRaw = other.responseRaw;
		dt = other.dt;
		count += other.count;
		fromCacheCnt += other.fromCacheCnt;
		if (other.questInfos.Count > 1) throw new Exception("bad");
		if (!questInfos.Contains(other.questInfos[0])) {
			questInfos.Add(other.questInfos[0]);
			questInfos.Sort();
		}
	}

	public void SetFromCache() {
		fromCacheCnt++;
	}

	public string ErrorInfo = "";

	public LogType log_type;
	public string domain = "";
	List<int> questInfos = new();
	public int count = 1;
	public DateTime dt;
	//public ReadOnlyMemory<byte> responseRaw; 
	public byte[] responseRaw; 

	public void SetReqType(int t) {
		questInfos.Clear();
		questInfos.Add(t);
	}

	static IImmutableSolidColorBrush c_block1 = new ImmutableSolidColorBrush(Color.Parse("#7792d1"));
	static IImmutableSolidColorBrush c_block2 = new ImmutableSolidColorBrush(Color.Parse("#edcc9d"));

	public IBrush? Background {
		get {
			return log_type switch {
				LogType.BlockedByPublicList => c_block1,
				LogType.BlockedByUserList => c_block2,
				LogType.AllowedByUserList => Brushes.GreenYellow,
				//LogType.Allow_PublicBlockListNotReady => Brushes.Gray,
				LogType.Block_PublicBlockListNotReady => Brushes.Gray,
				_ => default
			};
		}
	}

	public override string ToString() {
		var E = ErrorInfo == "" ? "" : " " + ErrorInfo;
		var types = string.Join(" / ", questInfos.Select(x => x switch { 1 => "A", 28 => "AAAA", _ => x.ToString() }));
		var cache = "";
		if (fromCacheCnt == count) cache = " CACHE";
		else if (fromCacheCnt > 0) cache = $" CACHE({fromCacheCnt})";
		return $"{dt.ToLocalQuick()} {log_type} {(count == 1 ? "" : $"({count})")} {domain} {types}{cache}{E}";
	}

}


