using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace AndromedaDnsFirewall.main;


// Крутится и работает исключительно в своем потоке. Единственный выход наружу - обновление полей модели (без событий) + NotifyIfNeed

internal static class ProcessTracer {

	static Dictionary<int, string> PidsToFullPath = new();
	static Dictionary<string, ProcessInfoModel> StableIdToInfo = new(); // пока храним бесконечно.

	static ProcessInfoModel GetProcessInfo(int pid, string name) {

		if (!PidsToFullPath.TryGetValue(pid, out var stableId)) {
			// внештатная ситуация не знаем про такой pid
			// поэтому за stableId у нас будет выступать name
			stableId = name;
		}
		if (!StableIdToInfo.TryGetValue(stableId, out var info)) {
			// новая запись
			info = new(name, stableId);
			StableIdToInfo.Add(stableId, info);
		} else if(info._lastUpdated.DeltToNow >= 12.hour()){
			// давно не обновлялись, чистим статистику
			info.ClearStatistics();
		}
		info.LastPid = pid; // обновим pid
		info._lastUpdated = TimePoint.Now;
		return info;
	}

	static void NotifyIfNeed(ProcessInfoModel info) {

		if (info._lastNotified.DeltToNow >= 1.sec()) {
			info._lastNotified = TimePoint.Now;
			// выполняем нотификацию, шедулим в gui поток прямо этот объект (без копирования)
			Dispatcher.UIThread.Post(() => { ProcessListModel.NotifyChanged(info); });
		}

	}

	static public void Start() {

		if (!ProgramUtils.IsElevated) {
			NotifyIfNeed(new ProcessInfoModel("Need admin rights", ""));
			return;
		}


		Task.Run(() => {
			do {
				try {
					DateTime dtStart = DateTime.UtcNow;
					using var session = new TraceEventSession("AndromedaNetMonitor");

					// Включаем ядро для отслеживания сети
					session.EnableKernelProvider(
						KernelTraceEventParser.Keywords.NetworkTCPIP
						| KernelTraceEventParser.Keywords.Process
						| KernelTraceEventParser.Keywords.ImageLoad
						);

					session.Source.Kernel.ProcessStart += (data) => {
						PidsToFullPath[data.ProcessID] = data.ImageFileName;
					};
					session.Source.Kernel.ProcessDCStart += (data) => {
						PidsToFullPath[data.ProcessID] = data.ImageFileName;
					};
					session.Source.Kernel.ImageLoad += (data) => {
						if (data.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
							PidsToFullPath[data.ProcessID] = data.FileName;
						}
					};
					session.Source.Kernel.ImageDCStart += (data) => {
						if (data.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
							PidsToFullPath[data.ProcessID] = data.FileName;
						}
					};
					session.Source.Kernel.ProcessStop += (data) => {
						PidsToFullPath.Remove(data.ProcessID);
					};

					// --- TCP: Ловим попытки установки соединений ---
					session.Source.Kernel.TcpIpConnect += (data) => {
						if (IPAddress.IsLoopback(data.daddr)) return;
						var cur = GetProcessInfo(data.ProcessID, data.ProcessName);
						cur.AddType(ProcessTraceType.TCP);
						NotifyIfNeed(cur);
					};
					session.Source.Kernel.TcpIpConnectIPV6 += (data) => {
						if (IPAddress.IsLoopback(data.daddr)) return;
						var cur = GetProcessInfo(data.ProcessID, data.ProcessName);
						cur.AddType(ProcessTraceType.TCP6);
						NotifyIfNeed(cur);
					};
					// --- UDP: Ловим отправку пакетов (т.к. у UDP нет 'Connect') ---
					session.Source.Kernel.UdpIpSend += (data) => {
						if (IPAddress.IsLoopback(data.daddr)) return;
						var cur = GetProcessInfo(data.ProcessID, data.ProcessName);
						cur.AddType(ProcessTraceType.UDP);
						NotifyIfNeed(cur);
					};
					session.Source.Kernel.UdpIpSendIPV6 += (data) => {
						if (IPAddress.IsLoopback(data.daddr)) return;
						var cur = GetProcessInfo(data.ProcessID, data.ProcessName);
						cur.AddType(ProcessTraceType.UDP6);
						NotifyIfNeed(cur);
					};
					session.Source.Kernel.UdpIpRecv += (data) => {
						if (IPAddress.IsLoopback(data.saddr)) return;
						var cur = GetProcessInfo(data.ProcessID, data.ProcessName);
						cur.AddType(ProcessTraceType.UDP);
						NotifyIfNeed(cur);
					};
					session.Source.Kernel.UdpIpRecvIPV6 += (data) => {
						if (IPAddress.IsLoopback(data.saddr)) return;
						var cur = GetProcessInfo(data.ProcessID, data.ProcessName);
						cur.AddType(ProcessTraceType.UDP6);
						NotifyIfNeed(cur);
					};

					// Запуск прослушивания
					session.Source.Process();
				} catch (Exception ex) {
					Dispatcher.UIThread.Post(() => {
						Log.Err(ex);
						GuiTools.ShowMessageNoWait(ex.Message); 

					});

				}
				//Thread.Sleep(1.min());
			} while (false);
		});
	}
}
