using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	void RegisterStatusRenderHook(IStatusRenderHook hook, double priority);
	void UnregisterStatusRenderHook(IStatusRenderHook hook);

	Color DefaultActiveStatusBarColor { get; }
	Color DefaultInactiveStatusBarColor { get; }
}

public interface IStatusRenderHook
{
	IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(State state, Combat combat, Ship ship) => Enumerable.Empty<(Status Status, double Priority)>();
	bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount) => null;
	bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount) => null;
	(IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount) => new();
	List<Tooltip> OverrideStatusTooltips(Status status, int amount, bool isForShipStatus, List<Tooltip> tooltips) => tooltips;
}