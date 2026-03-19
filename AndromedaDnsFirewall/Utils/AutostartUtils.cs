using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Linq;

namespace AutostartHandler;

public class AutostartUtils {

	string taskName;
	public AutostartUtils(string taskName) {
		this.taskName = taskName;
	}

	static RegistryKey CurRun => Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");

	public string Reg_GetAutostartExe() {
		var rkey = CurRun;
		var obj = rkey.GetValue(taskName);
		var val = obj as string;
		return val;
	}

	public void Reg_RemoveAutostart() {
		var rkey = CurRun;
		if (rkey.GetValue(taskName) != null)
			rkey.DeleteValue(taskName);

	}

	public void Reg_SetCurAutostart(string cmd) {
		var rkey = CurRun;
		rkey.SetValue(taskName, cmd);
	}

	public string? SH_GetAutostartExe() {
		using TaskService ts = new TaskService();
		var td = ts.RootFolder.Tasks.Where(x => x.Name == taskName).FirstOrDefault();
		if (td == null)
			return null;
		return td.Definition.Actions[0].ToString().Trim();
	}

	public void SH_RemoveAutostart() {
		using TaskService ts = new TaskService();
		ts.RootFolder.DeleteTask(taskName, false);
	}



	public void SH_SetCurAutostart(string path, string? args = null) {
		using TaskService ts = new TaskService();
		var td = ts.NewTask();
		td.RegistrationInfo.Description = "My task";
		td.Triggers.Add(new LogonTrigger() { });
		td.Actions.Add(new ExecAction(path, args, null));
		td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
		td.Settings.AllowDemandStart = true;
		td.Principal.RunLevel = TaskRunLevel.Highest;
		td.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero;
		td.Settings.StopIfGoingOnBatteries = false;
		td.Settings.DisallowStartIfOnBatteries = false;
		td.Settings.StartWhenAvailable = true;
		ts.RootFolder.DeleteTask(taskName, false);
		ts.RootFolder.RegisterTaskDefinition(taskName, td);
	}
}
