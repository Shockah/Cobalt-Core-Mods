using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using daisyowl.text;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class PrototypingCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Prototyping.png"), StableSpr.cards_Terminal).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototyping", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 2);

		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(new Hook());
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardUpgrade), nameof(CardUpgrade.FinallyReallyUpgrade)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardUpgrade_FinallyReallyUpgrade_Prefix))
		);
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 1, exhaust = true, floppable = true, description = ModEntry.Instance.Localizations.Localize(["card", "Prototyping", "description", upgrade.ToString(), flipped ? "flipped" : "normal"]) },
			a: () => new() { cost = 1, floppable = true, description = ModEntry.Instance.Localizations.Localize(["card", "Prototyping", "description", upgrade.ToString(), flipped ? "flipped" : "normal"]) },
			b: () => new() { cost = 1, exhaust = true, retain = true, description = ModEntry.Instance.Localizations.Localize(["card", "Prototyping", "description", upgrade.ToString()]) }
		);

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade.Switch<IReadOnlySet<ICardTraitEntry>>(
			none: () => ImmutableHashSet<ICardTraitEntry>.Empty,
			a: () => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Limited.Trait },
			b: () => ImmutableHashSet<ICardTraitEntry>.Empty
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () =>
			[
				new AAddCard { card = new PrototypeCard(), destination = CardDestination.Hand, disabled = flipped },
				new UpgradeAction { disabled = !flipped },
			],
			a: () =>
			[
				new AAddCard { card = new PrototypeCard(), destination = CardDestination.Hand, disabled = flipped },
				new UpgradeAction { disabled = !flipped },
			],
			b: () =>
			[
				new UpgradeAction(),
				new AAddCard { card = new PrototypeCard(), destination = CardDestination.Hand },
			]
		);
	
	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "FilterPrototypes", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterPrototypes"));
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, ref List<Card> __result)
	{
		var filterPrototypes = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterPrototypes");
		if (filterPrototypes is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (filterPrototypes is not null && __result[i] is PrototypeCard != filterPrototypes.Value)
				__result.RemoveAt(i);
	}
	
	private static bool CardUpgrade_FinallyReallyUpgrade_Prefix(CardUpgrade __instance, G g, Card newCard)
	{
		if (__instance is not InPlaceCardUpgrade)
			return true;

		var card = g.state.FindCard(newCard.uuid);
		if (card is null)
			return true;

		card.upgrade = newCard.upgrade;
		return false;
	}

	private sealed class UpgradeAction : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			
			var route = new CardBrowse { browseSource = CardBrowse.Source.Hand, browseAction = new InPlaceCardUpgradeBrowseAction() };
			ModEntry.Instance.Helper.ModData.SetModData(route, "FilterPrototypes", true);
			var cards = route.GetCardList(g);

			switch (cards.Count)
			{
				case 0:
					timer = 0;
					return baseResult;
				case 1:
					return new InPlaceCardUpgrade { cardCopy = cards[0] };
				default:
					return route;
			}
		}
	}

	private sealed class InPlaceCardUpgradeBrowseAction : CardAction
	{
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			if (selectedCard is null || selectedCard.upgrade != Upgrade.None)
			{
				timer = 0;
				return baseResult;
			}
			
			return new InPlaceCardUpgrade { cardCopy = Mutil.DeepCopy(selectedCard) };
		}
	}

	private sealed class InPlaceCardUpgrade : CardUpgrade;

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		{
			if (args.Card is not PrototypingCard)
				return null;
			return ModEntry.Instance.KokoroApi.Assets.PinchCompactFont;
		}
	}
}