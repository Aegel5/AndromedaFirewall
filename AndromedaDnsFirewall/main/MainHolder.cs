using AndromedaDnsFirewall.dns_server;
using AndromedaDnsFirewall.Utils;
using System;

namespace AndromedaDnsFirewall;

enum WorkMode {
	OnlyWhiteList = 0,
	AllExceptBlockList = 1,
	Bypass = 2
	//AllowAllWitoutLogs

}

internal class MainHolder {
	public static MainHolder Inst { get; private set; } = new();
	DnsServer server;

	public ObservableDeque<LogItem> logSource = new();

	public MainHolder() {
		server = new DnsServer { ProcessRequest = ProcessRequest };
	}

	public void Init() {
		server.Start();
	}

	LogType CalcLogType(string domain) {

		if (Config.Mode == WorkMode.Bypass)
			return LogType.Allowed;

		var userBlock = UserLists.GetDnsAction(domain);

		if (userBlock == RuleBlockAction.Block)
			return LogType.BlockedByUserList;

		if (userBlock == RuleBlockAction.Allow)
			return LogType.AllowedByUserList;

		if (!PublicBlockList.AllRecordsOk)
			return LogType.Block_PublicBlockListNotReady;

		if (PublicBlockList.IsNeedBlock(domain))
			return LogType.BlockedByPublicList;

		if (Config.Mode == WorkMode.OnlyWhiteList)
			return LogType.Blocked;

		return LogType.Allowed;
	}

	void InsertLogRecord(LogItem logitem) {

		try {

			bool found = false;

			for (int iLog = 0; iLog < Math.Min(7, logSource.Count); iLog++) {
				var cur = logSource[iLog];
				if (iLog > 0 && (logitem.dt - cur.dt).Duration().TotalSeconds > 10)
					break;
				if (cur.IsSame(logitem)) {
					cur.OverwriteWith(logitem);
					logSource.NotifyUpdated(iLog);
					found = true;
					break;
				}
			}

			if (!found) {
				logSource.PushFrontNotify(logitem);
				while (logSource.Count > 150) {
					logSource.PopBackNofity();
				}
			}
		} catch (Exception ex) {
			Log.Err(ex);
		}
	}

	async void ProcessRequest(ServerItem dnsItem) {

		ushort reqId = 0;
		LogItem logitem = new(); // запись в gui отвечающая за этот запрос.

		try {

			var raw_buf = dnsItem.req;
			logitem.packetSize = raw_buf.Length;
			reqId = DnsSimpleParser.ReadId(raw_buf);
			int questionsCount = DnsSimpleParser.QuestCount(raw_buf);

			string domain = "";
			int request_type = 0;

			// пробуем прочитать host и тип.
			if (questionsCount >= 1) {
				//var lazy_req = new LazyMessage(dnsItem.req);
				//var q = lazy_req.Msg.Questions[0];
				//domain = q.Name.ToCanonical().ToString();
				//request_type = (int)q.Type;

				var quest2 = DnsSimpleParser.ReadFirstQuest(raw_buf);
				domain = quest2.domain;
				request_type = quest2.type;
			}

			logitem.domain = domain;
			logitem.SetReqType(request_type);


			if (Config.Inst.mode == WorkMode.Bypass) {
				// simple bypass request
				logitem.log_type = LogType.Bypass;
				dnsItem.answ = await DnsResolver.Inst.RawResolveBypass(dnsItem.req);
				return; // go to finally 
			}

			if (domain == "") {
				logitem.log_type = LogType.BadRequest;
				logitem.ErrorInfo = "empty domain";
				return;
			}

			if (questionsCount != 1) {
				logitem.log_type = LogType.BadRequest;
				logitem.ErrorInfo = $"question count = {questionsCount}";
				return;
			}

			if (!DnsSimpleParser.IsQuery(raw_buf)) {
				logitem.log_type = LogType.BadRequest;
				logitem.ErrorInfo = $"not a query";
				return;
			}

			if (DnsSimpleParser.OpCode(raw_buf) != 0) {
				logitem.log_type = LogType.BadRequest;
				logitem.ErrorInfo = $"bad opcode";
				return;
			}

			logitem.log_type = CalcLogType(domain);

			// проверка блокировки
			if (logitem.log_type
				is LogType.BlockedByPublicList
				or LogType.BlockedByUserList
				or LogType.Block_PublicBlockListNotReady
				or LogType.Blocked
				) {
				return;
			}

			// Делаем resolve
			var (res, fromCache) = await DnsResolver.Inst.ResolveWithCache(raw_buf, domain, request_type);
			dnsItem.answ = res;
			if (fromCache) {
				logitem.SetFromCache();
			}

		} catch (Exception ex) {
			Log.Err(ex);
			logitem.log_type = LogType.Exception;
			logitem.ErrorInfo = ex.Message;
		} finally {

			try {
				if (dnsItem.answ == null) {
					dnsItem.answ = DnsSimpleParser.GenerateEmptyResponse(reqId);
				}
				await server.CompleteRequest(dnsItem);
			} catch (Exception ex) {
				Log.Err(ex);
				logitem.log_type = LogType.Exception;
				logitem.ErrorInfo = ex.Message;
			}

			InsertLogRecord(logitem);
		}
	}
}
