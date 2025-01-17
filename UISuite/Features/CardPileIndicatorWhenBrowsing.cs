using System;
using System.Collections.Generic;
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
	public CardPileIndicatorWhenBrowsingSettings CardPileIndicatorWhenBrowsing = new();

	internal sealed class CardPileIndicatorWhenBrowsingSettings
	{
		[JsonProperty]
		public CardBrowseCurrentPileSetting Display = CardBrowseCurrentPileSetting.Both;
		
		public enum CardBrowseCurrentPileSetting
		{
			Off, Tooltip, Icon, Both
		}
	}
}

internal sealed class CardPileIndicatorWhenBrowsing : IRegisterable
{
	private static ISpriteEntry InDrawPileIcon = null!;
	private static ISpriteEntry InDiscardPileIcon = null!;
	private static ISpriteEntry InExhaustPileIcon = null!;

	private static CardBrowse? RenderedCardBrowse;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		InDrawPileIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardPileIndicator/Draw.png"));
		InDiscardPileIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardPileIndicator/Discard.png"));
		InExhaustPileIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardPileIndicator/Exhaust.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Postfix)), priority: Priority.Last)
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IPluginPackage<IModManifest> package, IModSettingsApi api)
		=> api.MakeList([
			api.MakePadding(
				api.MakeText(
					() => ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Header"])
				).SetFont(DB.thicket),
				8,
				4
			),
			api.MakeEnumStepper(
				title: () => ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Title"]),
				getter: () => ModEntry.Instance.Settings.ProfileBased.Current.CardPileIndicatorWhenBrowsing.Display,
				setter: value => ModEntry.Instance.Settings.ProfileBased.Current.CardPileIndicatorWhenBrowsing.Display = value
			).SetValueFormatter(
				value => ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Value", value.ToString()])
			).SetValueWidth(
				_ => 60
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.CardPileIndicatorWhenBrowsing)}::{nameof(ProfileSettings.CardPileIndicatorWhenBrowsing.Display)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Title"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Settings", "Display", "Description"])
				}
			]),
		]);
	
	private static CardDestination? GetCardCurrentPile(State state, Combat? combat, Card card)
	{
		if (state.deck.Contains(card))
			return CardDestination.Deck;
		if (combat?.hand.Contains(card) ?? false)
			return CardDestination.Hand;
		if (combat?.discard.Contains(card) ?? false)
			return CardDestination.Discard;
		if (combat?.exhausted.Contains(card) ?? false)
			return CardDestination.Exhaust;
		return null;
	}
	
	private static Spr? GetCardDestinationIcon(CardDestination? destination)
		=> destination switch
		{
			CardDestination.Deck => InDrawPileIcon.Sprite,
			CardDestination.Hand => StableSpr.icons_dest_hand,
			CardDestination.Discard => InDiscardPileIcon.Sprite,
			CardDestination.Exhaust => InExhaustPileIcon.Sprite,
			_ => null
		};

	private static Tooltip? GetCardDestinationTooltip(CardDestination? destination)
	{
		var suffix = destination switch
		{
			CardDestination.Deck => "Draw",
			CardDestination.Hand => "Hand",
			CardDestination.Discard => "Discard",
			CardDestination.Exhaust => "Exhaust",
			_ => null
		};
		if (suffix is null)
			return null;

		return new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::CurrentPile::{suffix}")
		{
			Icon = GetCardDestinationIcon(destination),
			TitleColor = Colors.keyword,
			Title = ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Pile", suffix, "Title"]),
			Description = ModEntry.Instance.Localizations.Localize(["CardPileIndicatorWhenBrowsing", "Pile", suffix, "Description"]),
		};
	}

	private static void CardBrowse_Render_Prefix(CardBrowse __instance)
		=> RenderedCardBrowse = __instance.subRoute is null ? __instance : null;

	private static void CardBrowse_Render_Finalizer()
		=> RenderedCardBrowse = null;

	private static void Card_Render_Postfix(Card __instance, G g, Vec? posOverride)
	{
		if (RenderedCardBrowse is not { } cardBrowse)
			return;
		if (ModEntry.Instance.Settings.ProfileBased.Current.CardPileIndicatorWhenBrowsing.Display is ProfileSettings.CardPileIndicatorWhenBrowsingSettings.CardBrowseCurrentPileSetting.Off or ProfileSettings.CardPileIndicatorWhenBrowsingSettings.CardBrowseCurrentPileSetting.Tooltip)
			return;
		if (g.state.route is not Combat combat)
			return;
		if (cardBrowse.browseSource is CardBrowse.Source.Codex or CardBrowse.Source.DrawPile or CardBrowse.Source.DiscardPile or CardBrowse.Source.ExhaustPile or CardBrowse.Source.Hand)
			return;
		if (GetCardDestinationIcon(GetCardCurrentPile(g.state, combat, __instance)) is not { } icon)
			return;
		if (SpriteLoader.Get(icon) is not { } texture)
			return;

		var position = posOverride ?? __instance.pos;
		position += new Vec(0.0, __instance.hoverAnim * -2.0 + Mutil.Parabola(__instance.flipAnim) * -10.0 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * Math.Sign(__instance.flopAnim));
		position = position.round();

		Draw.Sprite(icon, position.x + 28 - texture.Width / 2, position.y + 75);
	}

	private static void Card_GetAllTooltips_Postfix(Card __instance, G g, ref IEnumerable<Tooltip> __result)
	{
		if (RenderedCardBrowse is not { } cardBrowse)
			return;
		if (ModEntry.Instance.Settings.ProfileBased.Current.CardPileIndicatorWhenBrowsing.Display is ProfileSettings.CardPileIndicatorWhenBrowsingSettings.CardBrowseCurrentPileSetting.Off or ProfileSettings.CardPileIndicatorWhenBrowsingSettings.CardBrowseCurrentPileSetting.Icon)
			return;
		if (g.state.route is not Combat combat)
			return;
		if (cardBrowse.browseSource is CardBrowse.Source.Codex or CardBrowse.Source.DrawPile or CardBrowse.Source.DiscardPile or CardBrowse.Source.ExhaustPile or CardBrowse.Source.Hand)
			return;
		if (GetCardDestinationTooltip(GetCardCurrentPile(g.state, combat, __instance)) is not { } tooltip)
			return;

		__result = [tooltip, .. __result];
	}
}