using System;
using System.Linq;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.UISuite;

// internal sealed partial class ProfileSettings
// {
// 	[JsonProperty]
// 	public CardPileIndicatorWhenBrowsingSettings CardPileIndicatorWhenBrowsing = new();
//
// 	internal sealed class CardPileIndicatorWhenBrowsingSettings
// 	{
// 		[JsonProperty]
// 		public CardBrowseCurrentPileSetting Display = CardBrowseCurrentPileSetting.Both;
// 		
// 		public enum CardBrowseCurrentPileSetting
// 		{
// 			Off, Tooltip, Icon, Both
// 		}
// 	}
// }

internal sealed class HeatAsBars : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.KokoroApi is null)
			return;
		
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderingHook());
	}

	// public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
	// 	=> api.MakeList([
	// 		api.MakePadding(
	// 			api.MakeText(
	// 				() => ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Header"])
	// 			).SetFont(DB.thicket),
	// 			8,
	// 			4
	// 		),
	// 		api.MakeEnumStepper(
	// 			title: () => ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Title"]),
	// 			getter: () => ModEntry.Instance.Settings.ProfileBased.Current.CardPileIndicatorWhenBrowsing.Display,
	// 			setter: value => ModEntry.Instance.Settings.ProfileBased.Current.CardPileIndicatorWhenBrowsing.Display = value
	// 		).SetValueFormatter(
	// 			value => ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Value", value.ToString()])
	// 		).SetValueWidth(
	// 			_ => 60
	// 		).SetTooltips(() => [
	// 			new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.CardPileIndicatorWhenBrowsing)}::{nameof(ProfileSettings.CardPileIndicatorWhenBrowsing.Display)}")
	// 			{
	// 				TitleColor = Colors.textBold,
	// 				Title = ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Title"]),
	// 				Description = ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Description"])
	// 			}
	// 		]),
	// 	]);

	private sealed class StatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		private static readonly StatusRenderer HeatRenderer = new((_, ship) => ship.heatTrigger, (_, ship) => ship.heatMin);
		private static readonly StatusRenderer OxidationRenderer = new((state, ship) => ModEntry.Instance.KokoroApi!.OxidationStatus.GetOxidationStatusThreshold(state, ship));
		
		public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
		{
			if (args.Status == Status.heat)
				return HeatRenderer;
			if (args.Status == ModEntry.Instance.KokoroApi!.OxidationStatus.Status)
				return OxidationRenderer;
			return null;
		}

		private sealed class StatusRenderer(
			Func<State, Ship, int> thresholdDelegate,
			Func<State, Ship, int>? negativeThresholdDelegate = null
		) : IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer
		{
			private static readonly IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer BarRenderer = ModEntry.Instance.KokoroApi!.StatusRendering.MakeBarStatusInfoRenderer();
			private static readonly IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer TextRenderer = ModEntry.Instance.KokoroApi!.StatusRendering.MakeTextStatusInfoRenderer("0").SetColor(Colors.redd);
			
			public int Render(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer.IRenderArgs args)
			{
				var totalWidth = 0;
				var threshold = thresholdDelegate(args.State, args.Ship);
				var negativeThreshold = negativeThresholdDelegate?.Invoke(args.State, args.Ship) ?? 0;
				var showingPositiveBars = threshold <= 15;
				
				var newArgs = args.CopyToBuilder();
				
				if (negativeThreshold != 0)
				{
					var rows = 1;
					while (rows < 3 && (-negativeThreshold + rows - 1) / rows > 5)
						rows++;
					
					BarRenderer.Rows = rows;
					BarRenderer.Segments = [
						.. Enumerable.Repeat(Colors.faint.fadeAlpha(0.3), Math.Clamp(args.Amount - negativeThreshold, 0, -negativeThreshold)),
						.. Enumerable.Repeat(Colors.faint, Math.Clamp(-args.Amount, 0, -negativeThreshold)),
					];
					BarRenderer.SegmentWidth = 1;
					newArgs.Position = new(args.Position.x + totalWidth, args.Position.y);
					totalWidth += BarRenderer.Render(newArgs);
				}
				
				if (showingPositiveBars)
				{
					if (totalWidth > 0)
						totalWidth -= 1;
					
					var rows = 1;
					while (rows < 3 && (threshold + rows - 1) / rows > 5)
						rows++;
					
					BarRenderer.Rows = rows;
					BarRenderer.Segments = [
						.. Enumerable.Repeat(Colors.redd, Math.Clamp(args.Amount, 0, threshold)),
						.. Enumerable.Repeat(Colors.redd.fadeAlpha(0.3), Math.Clamp(threshold - args.Amount, 0, threshold)),
					];
					BarRenderer.SegmentWidth = 2;
					newArgs.Position = new(args.Position.x + totalWidth, args.Position.y);
					totalWidth += BarRenderer.Render(newArgs);
				}

				if ((!showingPositiveBars && args.Amount > 0) || args.Amount > threshold)
				{
					if (totalWidth > 0)
						totalWidth += 1;

					TextRenderer.Text = showingPositiveBars ? $"+{args.Amount - threshold}" : $"{args.Amount}/{threshold}";
					newArgs.Position = new(args.Position.x + totalWidth, args.Position.y);
					totalWidth += TextRenderer.Render(newArgs);
				}

				return totalWidth;
			}
		}
	}
}