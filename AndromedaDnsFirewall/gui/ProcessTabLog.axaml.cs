using AndromedaDnsFirewall.main;
using Avalonia.Controls;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;


namespace AndromedaDnsFirewall;

public partial class ProcessTabLog : UserControl {

	string BuildInfo(IEnumerable<IPAddress> ips) {

		var dict = ips.ToDictionary(x => x, x => "");

		foreach (var logitem in MainHolder.Inst.logSource) {
			if (logitem.responseRaw == null) continue;
			try {
				Message parsed = new();
				parsed.Read(logitem.responseRaw);
				foreach (var item in parsed.Answers){
					if (item is AddressRecord addr) {
						var cur = addr.Address;
						if (dict.TryGetValue(cur, out var val) && val == "") {
							dict[cur] = logitem.domain;
						}
					}
				}
			} catch (Exception ex) {
				Log.Err(ex);
			}
		}

		return string.Join('\n', dict.Select(x => x.Value == "" ? x.Key.ToString() : $"{x.Key}({x.Value})"));
	}
	public ProcessTabLog() {
		InitializeComponent();

		cmd_copy.Command = GuiTools.CreateCommand(() => {
			var info = ge_logs.SelectedItem as ProcessInfoModel;
			if (info == null) return;
			var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
			if (clipboard != null) {
				clipboard.SetTextAsync(info.fullPath);
			}
		});
		cmd_info.Command = GuiTools.CreateCommand(() => {
			var info = ge_logs.SelectedItem as ProcessInfoModel;
			if (info == null) return;
			GuiTools.ShowMessageNoWait(BuildInfo(info.GetIPs()));
			
		});
		ge_logs.DoubleTapped += (a, b) => {
			var info = ge_logs.SelectedItem as ProcessInfoModel;
			if (info == null) return;
			GuiTools.ShowMessageNoWait(BuildInfo(info.GetIPs()));
		};


		ge_logs.ItemsSource = ProcessListModel.ModelBinding;
		//ge_logs2.ItemsSource = ProcessListModel.ModelBinding;
	}
}
