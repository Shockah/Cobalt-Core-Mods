using System.Reflection;
using System.Runtime.InteropServices;
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
	private static Font OriginalPinchFont = null!;
	private static Font OriginalPinchOutlineFont = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.KokoroApi is null)
			return;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			return;
		
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

	private static void BackupFonts()
	{
		OriginalPinchFont ??= DB.pinch;
		OriginalPinchOutlineFont ??= DB.pinch_outline;
	}

	private static bool RestoreFonts()
	{
		if (DB.pinch == OriginalPinchFont)
			return false;
		DB.pinch = OriginalPinchFont;
		DB.pinch_outline = OriginalPinchOutlineFont;
		return true;
	}

	private static bool SetCompactFonts()
	{
		if (DB.pinch == ModEntry.Instance.KokoroApi!.Assets.PinchCompactFont)
			return false;
		DB.pinch = ModEntry.Instance.KokoroApi!.Assets.PinchCompactFont;
		DB.pinch_outline = ModEntry.Instance.KokoroApi!.Assets.PinchCompactFont.outlined!;
		return true;
	}

	private static void Tooltip_RenderMultiple_Prefix(out bool __state)
	{
		BackupFonts();
		if (!ModEntry.Instance.Settings.ProfileBased.Current.CompactTooltipFont)
		{
			__state = false;
			return;
		}
		__state = SetCompactFonts();
	}

	private static void Tooltip_RenderMultiple_Finalizer(in bool __state)
	{
		if (__state)
			RestoreFonts();
	}

	private static void TTCard_Render_Prefix(out bool __state)
		=> __state = RestoreFonts();

	private static void TTCard_Render_Finalizer(in bool __state)
	{
		if (__state)
			SetCompactFonts();
	}
}