using Avalonia.Controls;

namespace AndromedaDnsFirewall;

public partial class UserBlockTab : UserControl {
	public static UserBlockTab Inst;
	public UserBlockTab() {
		Inst = this;
		InitializeComponent();

		cmd_delete.Click += (a, b) => {
			if (ge_logs.SelectedItem == null) return;
			var cur = (UserRuleModel?)ge_logs.SelectedItem;
			if (cur != null)
				UserLists.Delete(cur);
			Update();
		};
		cmd_allow.Click += (a, b) => {
			if (ge_logs.SelectedItem == null) return;
			var cur = (UserRuleModel?)ge_logs.SelectedItem;
			if (cur != null) {
				cur.Action = RuleBlockAction.Allow;
				UserLists.Save();
			}
			Update();
		};
		cmd_block.Click += (a, b) => {
			if (ge_logs.SelectedItem == null) return;
			var cur = (UserRuleModel?)ge_logs.SelectedItem;
			if (cur != null) {
				cur.Action = RuleBlockAction.Block;
				UserLists.Save();
			}
			Update();
		};
	}

	public void Update() {
		ge_logs.ItemsSource = null;
		ge_logs.ItemsSource = UserLists.userRules.Values;
	}
}
