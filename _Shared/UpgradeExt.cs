using System;

namespace Shockah.Shared;

internal static class UpgradeExt
{
	public static T Switch<T>(this Upgrade upgrade, Func<T> none, Func<T> a, Func<T> b)
		=> upgrade switch
		{
			Upgrade.A => a(),
			Upgrade.B => b(),
			_ => none()
		};
}