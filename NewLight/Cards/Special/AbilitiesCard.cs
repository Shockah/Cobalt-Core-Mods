using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class AbilitiesCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = Rarity.uncommon,
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Special", "Abilities", "Name"]).Localize,
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetFullDisplayName)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetFullDisplayName_Postfix))
		);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with
		{
			cost = 0, singleUse = true, temporary = true,
			description = ModEntry.Instance.Localizations.Localize(["Card", "Special", "Abilities", "Description"]),
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			ModEntry.Instance.KokoroApi.CustomCardBrowseSource
				.ModifyCardSelect(new ACardSelect { browseAction = new BrowseAction() })
				.SetCustomBrowseSource(new BrowseSource()).AsCardAction,
		];

	private static void Card_GetFullDisplayName_Postfix(Card __instance, ref string __result)
	{
		if (__instance is not AbilitiesCard)
			return;
		if (MG.inst.g.state is not { } state)
			return;
		if (state.route is not Combat combat)
			return;

		var abilityCount = combat.Abilities.Count(card => Abilities.GetCurrentCooldown(state, combat, card) <= 0);
		if (abilityCount <= 0)
			return;

		__result = ModEntry.Instance.Localizations.Localize(["Card", "Special", "Abilities", "StatefulFormat"], new { FullName = __result, Count = abilityCount });
	}

	private sealed class BrowseAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;
			
			s.RemoveCardFromWhereverItIs(selectedCard.uuid);
			c.SendCardToHand(s, selectedCard);
			Audio.Play(Event.CardHandling);
		}
	}
	
	private sealed class BrowseSource : IKokoroApi.IV2.ICustomCardBrowseSourceApi.ICustomCardBrowseSource
	{
		public IReadOnlyList<Tooltip> GetSearchTooltips(State state)
			=> [new GlossaryTooltip("action.searchCardNew")
			{	
				Icon = StableSpr.icons_searchCardNew,
				TitleColor = Colors.action,
				Title = Loc.T("action.searchCardNew.name"),
				Description = ModEntry.Instance.Localizations.Localize(["Card", "Special", "Abilities", "SearchAction", "Description"]),
			}];

		public string GetTitle(State state, Combat? combat, IReadOnlyList<Card> cards)
			=> ModEntry.Instance.Localizations.Localize(["Card", "Special", "Abilities", "SearchAction", "Title"], new { Count = cards.Count });

		public IReadOnlyList<Card> GetCards(State state, Combat? combat)
			=> combat?.Abilities.Where(card => Abilities.GetCurrentCooldown(state, combat, card) <= 0).ToList() ?? [];
	}
}