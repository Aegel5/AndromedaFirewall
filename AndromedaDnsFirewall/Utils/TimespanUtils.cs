using System;

namespace AndromedaDnsFirewall;

public static class TimespanExtensions {
	public static TimeSpan sec(this int t) {
		return TimeSpan.FromSeconds(t);
	}

	public static TimeSpan min(this int t) {
		return TimeSpan.FromMinutes(t);
	}

	public static TimeSpan hour(this int t) {
		return TimeSpan.FromHours(t);
	}

	public static TimeSpan msec(this int t) {
		return TimeSpan.FromMilliseconds(t);
	}
}
