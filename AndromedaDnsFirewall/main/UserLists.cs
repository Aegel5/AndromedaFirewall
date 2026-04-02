using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AndromedaDnsFirewall;


internal static class UserLists {

	static readonly string path = Path.Combine(ProgramUtils.BinFolder, "UserRules.json");

	public static void Save() {
		using var stream = File.Create(path);
		JsonSerializer.Serialize(stream, userRules.Values, Opt); // blocks this thread!
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
	public static void Load() {
		if (!File.Exists(path)) return;
		using var stream = File.OpenRead(path);
		var res = JsonSerializer.Deserialize<List<UserRuleModel>>(stream, Opt);
		if (res != null)
			userRules = res.ToDictionary(x => x.Target);
	}

	public static void Delete(UserRuleModel entry) {
		userRules.Remove(entry.Target);
		Save();
	}

	public static void BlockDns(string host) {
		userRules.GetOrAdd(host, k => new(host)).Action = RuleBlockAction.Block;
		Save();
	}

	public static void AllowDns(string host) {
		userRules.GetOrAdd(host, k => new(host)).Action = RuleBlockAction.Allow;
		Save();
	}

	public static RuleBlockAction? GetDnsAction(string domain) {
		if (userRules.TryGetValue(domain, out var res)) {
			return res.Action;
		}
		return null;
	}

	static public Dictionary<string, UserRuleModel> userRules = new();

}
