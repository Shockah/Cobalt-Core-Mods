using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		Pulse();
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
	{
		List<CardAction> actions = new();

		List<Status> statuses = new() { Status.tempShield, Status.shield };
		var booksDizzyArtifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
		if (booksDizzyArtifact is not null)
			statuses.Add(Status.shard);

		if (!TryToPay(s.ship, statuses, 3, out var toPay))
			return actions;

		foreach (var (status, toTake) in toPay)
		{
			c.Queue(new AStatus
			{
				status = status,
				statusAmount = -toTake,
				targetPlayer = true
			});

			if (booksDizzyArtifact is not null && status == Status.shard)
				booksDizzyArtifact.Pulse();
		}
		c.QueueImmediate(new AStatus
		{
			status = Status.boost,
			statusAmount = 1,
			targetPlayer = true
		});

		return actions;
	}

	private static bool TryToPay(Ship ship, List<Status> statuses, int amount, [MaybeNullWhen(false)] out Dictionary<Status, int> toPay)
	{
		if (statuses.Select(ship.Get).Sum() < amount)
		{
			toPay = null;
			return false;
		}

		toPay = new();
		foreach (var status in statuses)
		{
			int toTake = Math.Min(amount, ship.Get(status));
			amount -= toTake;
			toPay[status] = toTake;

			if (amount <= 0)
				break;
		}
		return true;
	}
}