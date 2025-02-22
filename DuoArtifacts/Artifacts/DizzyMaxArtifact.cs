using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;

namespace Shockah.DuoArtifacts;

internal sealed class DizzyMaxArtifact : DuoArtifact
{
	protected internal override void RegisterCards(ICardRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterCards(registry, namePrefix, definition);
		ExternalCard card = new(
			$"{namePrefix}.DizzyMaxArtifactCard",
			typeof(DizzyMaxArtifactCard),
			ExternalSprite.GetRaw((int)StableSpr.cards_Terminal),
			Instance.Database.DuoArtifactDeck
		);
		card.AddLocalisation(I18n.DizzyMaxArtifactCardName);
		registry.RegisterCard(card);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTCard { card = new DizzyMaxArtifactCard() }];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.QueueImmediate(new AAddCard
		{
			card = new DizzyMaxArtifactCard(),
			destination = CardDestination.Hand,
			artifactPulse = Key(),
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
			flippable = true,
			unplayable = state != DB.fakeState && !TryToPay(state.ship, GetPayingStatuses(state).ToList(), 3, out _),
			description = I18n.DizzyMaxArtifactCardDescription,
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var actions = new List<CardAction>();
		var statuses = GetPayingStatuses(s).ToList();
		var booksDizzyArtifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);

		if (!TryToPay(s.ship, statuses, 3, out var toPay))
			return actions;

		foreach (var (status, toTake) in toPay)
		{
			actions.Add(new AStatus
			{
				status = status,
				statusAmount = -toTake,
				targetPlayer = true,
			});

			if (booksDizzyArtifact is not null && status == Status.shard)
				booksDizzyArtifact.Pulse();
		}
		actions.Add(new AStatus
		{
			status = Status.boost,
			statusAmount = 1,
			targetPlayer = true,
		});

		return actions;
	}

	public override void OnFlip(G g)
	{
		base.OnFlip(g);
		if (g.state.route is not Combat combat)
			return;

		var index = combat.hand.IndexOf(this);
		if (index == -1)
			return;

		combat.hand.Remove(this);
		if (index == 0)
			combat.hand.Add(this);
		else
			combat.hand.Insert(0, this);
	}
	
	private static IEnumerable<Status> GetPayingStatuses(State state)
	{
		yield return Status.tempShield;
		yield return Status.shield;

		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact) is not null)
			yield return Status.shard;
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
			var toTake = Math.Min(amount, ship.Get(status));
			amount -= toTake;
			toPay[status] = toTake;

			if (amount <= 0)
				break;
		}
		return true;
	}
}