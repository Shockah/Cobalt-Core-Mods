using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DizzyMaxArtifact : DuoArtifact
{
	protected internal override void RegisterCards(ICardRegistry registry, string namePrefix)
	{
		base.RegisterCards(registry, namePrefix);
		ExternalCard card = new(
			$"{namePrefix}.DizzyMaxArtifactCard",
			typeof(DizzyMaxArtifactCard),
			ExternalSprite.GetRaw((int)StableSpr.cards_Terminal),
			ExternalDeck.GetRaw((int)Deck.colorless)
		);
		card.AddLocalisation(I18n.DizzyMaxArtifactCardName);
		registry.RegisterCard(card);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Insert(0, new TTCard { card = new DizzyMaxArtifactCard() });
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.QueueImmediate(new AAddCard
		{
			card = new DizzyMaxArtifactCard(),
			destination = CardDestination.Hand
		});
	}
}

[CardMeta(dontOffer = true)]
internal sealed class DizzyMaxArtifactCard : Card
{
	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			infinite = true,
			temporary = true,
			retain = true,
			description = I18n.DizzyMaxArtifactCardDescription
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new ADelegateAction((_, s, _) =>
			{
				if (TryToPayAndTrigger(s.ship, Status.tempShield))
					return;
				if (TryToPayAndTrigger(s.ship, Status.shield))
					return;

				var booksDizzyArtifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
				if (booksDizzyArtifact is null)
					return;

				if (TryToPayAndTrigger(s.ship, Status.shard))
					booksDizzyArtifact.Pulse();
			})
		};

	private static bool TryToPayAndTrigger(Ship ship, Status status)
	{
		var success = TryToPay(ship, status);
		if (success)
			ship.Add(Status.boost);
		return success;
	}

	private static bool TryToPay(Ship ship, Status status)
	{
		if (ship.Get(status) > 0)
		{
			ship.Add(status, -1);
			return true;
		}
		return false;
	}
}