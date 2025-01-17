using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.UISuite;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool AnchorCardPileOverlay = true;
}

internal sealed class AnchorCardPileOverlay : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDeck)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDeck_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDiscard)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDiscard_Postfix))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => ModEntry.Instance.Localizations.Localize(["AnchorCardPileOverlay", "Settings", "Header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeCheckbox(
				() => ModEntry.Instance.Localizations.Localize(["AnchorCardPileOverlay", "Settings", "IsEnabled", "Title"]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.AnchorCardPileOverlay,
				(_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.AnchorCardPileOverlay = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.AnchorCardPileOverlay)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["AnchorCardPileOverlay", "Settings", "IsEnabled", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["AnchorCardPileOverlay", "Settings", "IsEnabled", "Description"]),
				},
				new TTCard { card = new TrashAnchor() },
			]),
		]);

	private static void RenderAnchorOverlayIfNeeded(G g, UIKey uiKey, List<Card> cards)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.AnchorCardPileOverlay)
			return;
		if (!cards.Any(card => card is TrashAnchor))
			return;
		if (g.boxes.FirstOrDefault(b => b.key == uiKey) is not { } box)
			return;
		
		var texture = SpriteLoader.Get(StableSpr.cards_Anchor_Overlay)!;
		Draw.Sprite(texture, box.rect.x2 - texture.Width + 2, box.rect.y - 2);

		if (!box.IsHover())
			return;

		g.tooltips.Add(g.tooltips.pos, new TTText(ModEntry.Instance.Localizations.Localize(["AnchorCardPileOverlay", "Tooltip"])));
	}

	private static void Combat_RenderDeck_Postfix(G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		RenderAnchorOverlayIfNeeded(g, StableUK.combat_deck, g.state.deck);
	}

	private static void Combat_RenderDiscard_Postfix(Combat __instance, G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		RenderAnchorOverlayIfNeeded(g, StableUK.combat_discard, __instance.discard);
	}
}