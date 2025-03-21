using System;
using System.Linq;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;
using Shockah.Kokoro;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public StatusAsBarSettings HeatAsBars = new();
	
	[JsonProperty]
	public StatusAsBarSettings OxidationAsBars = new();

	internal sealed class StatusAsBarSettings
	{
		[JsonProperty]
		public bool IsEnabled = true;
		
		[JsonProperty]
		public bool SwitchToTwoRows = true;
		
		[JsonProperty]
		public int SwitchToTwoRowsAt = 6;
		
		[JsonProperty]
		public bool SwitchToThreeRows = true;
		
		[JsonProperty]
		public int SwitchToThreeRowsAt = 11;
		
		[JsonProperty]
		public bool SwitchToNumbers = true;
		
		[JsonProperty]
		public int SwitchToNumbersAt = 16;
	}
}

internal sealed class StatusAsBars : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.KokoroApi is null)
			return;
		
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderingHook());
	}

	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
	{
		return api.MakeList([
			MakeSettingsForStatus("Heat", s => s.HeatAsBars),
			MakeSettingsForStatus("Oxidation", s => s.OxidationAsBars),
		]);

		IModSettingsApi.IModSetting MakeSettingsForStatus(string headerKey, Func<ProfileSettings, ProfileSettings.StatusAsBarSettings> settings)
			=> api.MakeList([
				api.MakePadding(
					api.MakeText(
						() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "Header", headerKey])
					).SetFont(DB.thicket),
					8,
					4
				),
				api.MakeCheckbox(
					() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "IsEnabled", "Title"]),
					() => settings(ModEntry.Instance.Settings.ProfileBased.Current).IsEnabled,
					(_, _, value) => settings(ModEntry.Instance.Settings.ProfileBased.Current).IsEnabled = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.StatusAsBarSettings)}::{nameof(ProfileSettings.StatusAsBarSettings.IsEnabled)}")
					{
						TitleColor = Colors.textBold,
						Title = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "IsEnabled", "Title"]),
						Description = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "IsEnabled", "Description"]),
					},
				]),
				api.MakeConditional(
					api.MakeList([
						api.MakeCheckbox(
							() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToTwoRows", "Title"]),
							() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToTwoRows,
							(_, _, value) => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToTwoRows = value
						).SetTooltips(() => [
							new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.StatusAsBarSettings)}::{nameof(ProfileSettings.StatusAsBarSettings.SwitchToTwoRows)}")
							{
								TitleColor = Colors.textBold,
								Title = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToTwoRows", "Title"]),
								Description = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToTwoRows", "Description"]),
							},
						]),
						api.MakeConditional(
							api.MakeNumericStepper(
								() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToTwoRowsAt", "Title"]),
								() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToTwoRowsAt,
								value => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToTwoRowsAt = value,
								minValue: 0
							),
							() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToTwoRows
						),
						api.MakeCheckbox(
							() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToThreeRows", "Title"]),
							() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToThreeRows,
							(_, _, value) => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToThreeRows = value
						).SetTooltips(() => [
							new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.StatusAsBarSettings)}::{nameof(ProfileSettings.StatusAsBarSettings.SwitchToThreeRows)}")
							{
								TitleColor = Colors.textBold,
								Title = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToThreeRows", "Title"]),
								Description = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToThreeRows", "Description"]),
							},
						]),
						api.MakeConditional(
							api.MakeNumericStepper(
								() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToThreeRowsAt", "Title"]),
								() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToThreeRowsAt,
								value => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToThreeRowsAt = value,
								minValue: 0
							),
							() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToThreeRows
						),
						api.MakeCheckbox(
							() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToNumbers", "Title"]),
							() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToNumbers,
							(_, _, value) => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToNumbers = value
						).SetTooltips(() => [
							new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.StatusAsBarSettings)}::{nameof(ProfileSettings.StatusAsBarSettings.SwitchToNumbers)}")
							{
								TitleColor = Colors.textBold,
								Title = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToNumbers", "Title"]),
								Description = ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToNumbers", "Description"]),
							},
						]),
						api.MakeConditional(
							api.MakeNumericStepper(
								() => ModEntry.Instance.Localizations.Localize(["StatusAsBars", "Settings", "SwitchToNumbersAt", "Title"]),
								() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToNumbersAt,
								value => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToNumbersAt = value,
								minValue: 0
							),
							() => settings(ModEntry.Instance.Settings.ProfileBased.Current).SwitchToNumbers
						),
					]),
					() => settings(ModEntry.Instance.Settings.ProfileBased.Current).IsEnabled
				),
			]);
	}

	private sealed class StatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		private static readonly StatusRenderer HeatRenderer = new(
			(_, ship) => ship.heatTrigger,
			(_, ship) => ship.heatMin,
			() => ModEntry.Instance.Settings.ProfileBased.Current.HeatAsBars
		);
		private static readonly StatusRenderer OxidationRenderer = new(
			(state, ship) => ModEntry.Instance.KokoroApi!.OxidationStatus.GetOxidationStatusThreshold(state, ship),
			null,
			() => ModEntry.Instance.Settings.ProfileBased.Current.OxidationAsBars
		);
		
		public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
		{
			if (args.Status == Status.heat)
				return ModEntry.Instance.Settings.ProfileBased.Current.HeatAsBars.IsEnabled ? HeatRenderer : null;
			if (args.Status == ModEntry.Instance.KokoroApi!.OxidationStatus.Status)
				return ModEntry.Instance.Settings.ProfileBased.Current.OxidationAsBars.IsEnabled ? OxidationRenderer : null;
			return null;
		}

		private sealed class StatusRenderer(
			Func<State, Ship, int> thresholdDelegate,
			Func<State, Ship, int>? negativeThresholdDelegate,
			Func<ProfileSettings.StatusAsBarSettings> settingsDelegate
		) : IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer
		{
			private static readonly IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer BarRenderer = ModEntry.Instance.KokoroApi!.StatusRendering.MakeBarStatusInfoRenderer();
			private static readonly IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer TextRenderer = ModEntry.Instance.KokoroApi!.StatusRendering.MakeTextStatusInfoRenderer("0").SetColor(Colors.redd);
			
			public int Render(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer.IRenderArgs args)
			{
				var settings = settingsDelegate();
				
				var totalWidth = 0;
				var threshold = thresholdDelegate(args.State, args.Ship);
				var negativeThreshold = negativeThresholdDelegate?.Invoke(args.State, args.Ship) ?? 0;
				var showingPositiveBars = !settings.SwitchToNumbers || threshold < settings.SwitchToNumbersAt;
				
				var newArgs = args.CopyToBuilder();
				
				if (negativeThreshold != 0)
				{
					BarRenderer.Rows = GetRowsForThreshold(-negativeThreshold);
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
					
					BarRenderer.Rows = GetRowsForThreshold(threshold);
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

				int GetRowsForThreshold(int threshold)
				{
					var rows = 1;
					if (settings.SwitchToTwoRows && threshold >= settings.SwitchToTwoRowsAt)
						rows = 2;
					if (settings.SwitchToThreeRows && threshold >= settings.SwitchToThreeRowsAt)
						rows = 3;
					return rows;
				}
			}
		}
	}
}