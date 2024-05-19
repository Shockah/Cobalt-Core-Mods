﻿using System.Collections.Generic;

namespace Shockah.Dracula;

public partial interface IKokoroApi
{
	void RegisterStatusRenderHook(IStatusRenderHook hook, double priority);
	void UnregisterStatusRenderHook(IStatusRenderHook hook);

	Color DefaultActiveStatusBarColor { get; }
	Color DefaultInactiveStatusBarColor { get; }
}

public interface IStatusRenderHook
{
	bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount) => null;
	bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount) => null;
	(IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount) => new();
	List<Tooltip> OverrideStatusTooltips(Status status, int amount, bool isForShipStatus, List<Tooltip> tooltips) => tooltips;
}