using AndromedaDnsFirewall.gui;

namespace AndromedaDnsFirewall;

internal enum RuleBlockAction {
	Block,
	Allow
}

internal class UserRuleModel : ViewModelBase {

	public RuleBlockAction Action { get; set; } = RuleBlockAction.Block;
	public string Target { get => field; set => SetProperty(ref field, value); } = "";
	public string Comment { get => field; set => SetProperty(ref field, value); } = "";
	public bool Enabled { get => field; set => SetProperty(ref field, value); } = true;

	public override string ToString() {
		return $"{Action} {Target} {Comment}";
	}

	public UserRuleModel(string target) {
		this.Target = target;
	}
}
