using AutostartHandler;
using Avalonia.Controls;
using System;


namespace AndromedaDnsFirewall;

public partial class SettingsTab : UserControl {

	string taskName = "AndromedaTask_FJD453431";

	void UpdateAutostart() {
		var path = ProgramUtils.ExePath;
		var checker = new AutostartUtils(taskName);
		var is_reg = checker.Reg_GetAutostartExe() == path;
		var s = checker.SH_GetAutostartExe();
		var is_sh = s == path;
		ge_autostart.IsChecked = Config.Inst.AddToAutostart = ProgramUtils.IsElevated ? !is_reg && is_sh : is_reg && !is_sh;
	}
	void SetAutostart() {
		try {
			var path = ProgramUtils.ExePath;
			var checker = new AutostartUtils(taskName);
			var want = Config.Inst.AddToAutostart;

			// удаляем
			if (!want || ProgramUtils.IsElevated) {
				checker.Reg_RemoveAutostart();
			}
			if (!want && ProgramUtils.IsElevated) {
				checker.SH_RemoveAutostart();
			}

			// добавляем
			if (want) {
				if (ProgramUtils.IsElevated) {
					checker.SH_SetCurAutostart(path);
				} else {
					checker.Reg_SetCurAutostart(path);
				}
			}
		} catch (Exception ex) {
			Log.Err(ex);
		}
	}
	public SettingsTab() {
		InitializeComponent();
		ge_autostart.Click += (a, b) => {
			Config.Inst.AddToAutostart = !Config.Inst.AddToAutostart;
			SetAutostart();
			UpdateAutostart();
		};
		UpdateAutostart();
	}
}
