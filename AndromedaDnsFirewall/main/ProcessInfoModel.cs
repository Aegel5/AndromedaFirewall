using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AndromedaDnsFirewall.main;

enum ProcessTraceType  {
	TCP = 0, 
	TCP6 = 1, 
	UDP = 2, 
	UDP6 = 3
}

// Мульти-поточный класс - наш мостик между трейсером и остальной программой
// Пока после создания вообще никогда не удаляется
// Пишется и создается только из трейсера. Читается откуда угодно (на volatile забиваем).
class ProcessInfoModel {
	[InlineArray(4)] public struct TCounts { private int _firstElement; }

	public string Name = "";
	public string fullPath = "";
	public int LastPid {
		get => Volatile.Read(ref field);
		set => Interlocked.Exchange(ref field, value);
	}
	TCounts counts;

	public TimePoint _lastNotified;

	public TimePoint _lastUpdated;

	public void ClearStatistics() {
		counts = new();
	}

	public ProcessInfoModel(string name, string fullname) {
		this.Name = name;
		this.fullPath = fullname;
	}


	public void AddType(ProcessTraceType type) {
		Interlocked.Increment(ref counts[(int)type]);
	}
	public int GetCount(ProcessTraceType type) => Volatile.Read(ref counts[(int)type]);

	public override string ToString() {
		// пока используем строку
		string build(ProcessTraceType t) {
			var cnt = counts[(int)t];
			return cnt == 0 ? "" : $" {t}:{cnt}";
		}
		return $"PID:{LastPid} {Name}{build(ProcessTraceType.TCP)}{build(ProcessTraceType.TCP6)}{build(ProcessTraceType.UDP)}{build(ProcessTraceType.UDP6)} {fullPath}";
	}
}
