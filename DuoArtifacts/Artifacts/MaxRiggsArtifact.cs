using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

internal sealed class MaxRiggsArtifact : DuoArtifact
{
	protected internal override void RegisterCards(ICardRegistry registry, string namePrefix)
	{
		base.RegisterCards(registry, namePrefix);
		ExternalCard card = new(
			$"{namePrefix}.MaxRiggsArtifactCard",
			typeof(MaxRiggsArtifactCard),
			ExternalSprite.GetRaw((int)StableSpr.cards_BranchPrediction),
			ExternalDeck.GetRaw((int)Deck.colorless)
		);
		card.AddLocalisation(I18n.MaxRiggsArtifactCardName);
		registry.RegisterCard(card);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTCard { card = new MaxRiggsArtifactCard() });
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.QueueImmediate(new AAddCard
		{
			card = new MaxRiggsArtifactCard(),
			destination = CardDestination.Hand
		});
	}
}

[CardMeta(dontOffer = true)]
internal sealed class MaxRiggsArtifactCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			temporary = true,
			exhaust = true,
			retain = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = Status.autododgeLeft,
				statusAmount = 1,
				targetPlayer = true
			}
		};
}