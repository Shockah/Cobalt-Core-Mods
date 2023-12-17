using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeMaxArtifact : DuoArtifact
{
	protected internal override void RegisterCards(ICardRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterCards(registry, namePrefix, definition);
		ExternalCard card = new(
			$"{namePrefix}.DrakeMaxArtifactCard",
			typeof(DrakeMaxArtifactCard),
			ExternalSprite.GetRaw((int)StableSpr.cards_hacker),
			Instance.Database.DuoArtifactDeck
		);
		card.AddLocalisation(I18n.DrakeMaxArtifactCardName);
		registry.RegisterCard(card);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTCard { card = new DrakeMaxArtifactCard() });
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Pulse();
		combat.Queue(new AAddCard
		{
			card = new DrakeMaxArtifactCard(),
			destination = CardDestination.Deck
		});
		combat.Queue(new AAddCard
		{
			card = new WormFood { temporaryOverride = true },
			destination = CardDestination.Deck
		});
	}
}

[CardMeta(dontOffer = true)]
internal sealed class DrakeMaxArtifactCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			temporary = true,
			retain = true,
			exhaust = true,
			description = I18n.DrakeMaxArtifactCardDescription
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var cards = s.deck
			.Concat(c.discard)
			.Concat(c.hand)
			.OfType<WormFood>()
			.ToList();

		List<CardAction> actions = new();
		foreach (var card in cards)
			actions.Add(new AExhaustWherever { uuid = card.uuid });
		actions.Add(new AStatus
		{
			status = (Status)ModEntry.Instance.KokoroApi.WormStatus.Id!.Value,
			statusAmount = cards.Count,
			targetPlayer = false
		});
		return actions;
	}
}