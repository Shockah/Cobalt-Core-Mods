using System.Collections.Generic;
using System.Reflection;
using daisyowl.text;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool CompactTooltipFont;
}

internal sealed class CompactTooltipFont : IRegisterable
{
	private static readonly List<(Font Base, Font Outline)> FontStack = [];
	private static Font OriginalPinchFont = null!;
	private static Font OriginalPinchOutlineFont = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.KokoroApi is null)
			return;

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			
			OriginalPinchFont = DB.pinch;
			OriginalPinchOutlineFont = DB.pinch_outline;
		};
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Tooltip), nameof(Tooltip.RenderMultiple)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Tooltip_RenderMultiple_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Tooltip_RenderMultiple_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(TTCard), nameof(TTCard.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(TTCard_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(TTCard_Render_Finalizer))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
	{
		if (ModEntry.Instance.KokoroApi is null)
			return api.MakeList([]);

		return api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => ModEntry.Instance.Localizations.Localize(["CompactTooltipFont", "Settings", "Header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["CompactTooltipFont", "Settings", "IsEnabled", "Title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.CompactTooltipFont,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.CompactTooltipFont = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.CompactTooltipFont)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["CompactTooltipFont", "Settings", "IsEnabled", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["CompactTooltipFont", "Settings", "IsEnabled", "Description"]),
				},
			]),
		]);
	}

	private static void PushFonts(bool compact)
	{
		FontStack.Add((DB.pinch, DB.pinch_outline));
		DB.pinch = compact ? ModEntry.Instance.KokoroApi!.Assets.PinchCompactFont : OriginalPinchFont;
		DB.pinch_outline = compact ? ModEntry.Instance.KokoroApi!.Assets.PinchCompactFont.outlined! : OriginalPinchOutlineFont;
	}

	private static void PopFonts()
	{
		if (FontStack.Count == 0)
			return;

		var fonts = FontStack[^1];
		FontStack.RemoveAt(FontStack.Count - 1);
		
		DB.pinch = fonts.Base;
		DB.pinch_outline = fonts.Outline;
	}

	private static void Tooltip_RenderMultiple_Prefix()
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CompactTooltipFont)
			return;
		PushFonts(compact: true);
	}

	private static void Tooltip_RenderMultiple_Finalizer()
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CompactTooltipFont)
			return;
		PopFonts();
	}

	private static void TTCard_Render_Prefix()
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CompactTooltipFont)
			return;
		PushFonts(compact: false);
	}

	private static void TTCard_Render_Finalizer()
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CompactTooltipFont)
			return;
		PopFonts();
	}
}