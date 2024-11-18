using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IStatusRenderingApi StatusRendering { get; }

		public interface IStatusRenderingApi
		{
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			Color DefaultActiveStatusBarColor { get; }
			Color DefaultInactiveStatusBarColor { get; }
			
			public interface IHook : IKokoroV2ApiHook
			{
				IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(State state, Combat combat, Ship ship) => [];
				bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount) => null;
				bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount) => null;
				(IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount) => new();
				List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips) => tooltips;
			}
		}
	}
}
