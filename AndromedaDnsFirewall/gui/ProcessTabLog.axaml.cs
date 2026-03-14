using AndromedaDnsFirewall.main;
using Avalonia.Controls;


namespace AndromedaDnsFirewall;

public partial class ProcessTabLog : UserControl {
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

		ge_logs.ItemsSource = ProcessListModel.ModelBinding;
		//ge_logs2.ItemsSource = ProcessListModel.ModelBinding;
	}
}
