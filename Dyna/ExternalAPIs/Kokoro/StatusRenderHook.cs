using System.Collections.Generic;

namespace Shockah.Dyna;

public partial interface IKokoroApi
{
	void RegisterStatusRenderHook(IStatusRenderHook hook, double priority);
	void UnregisterStatusRenderHook(IStatusRenderHook hook);
}

public interface IStatusRenderHook
{
	List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips) => tooltips;
}