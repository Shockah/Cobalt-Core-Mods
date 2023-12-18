using System;
using System.Collections.Generic;

namespace Shockah.ABUpgrades;

public sealed class ApiImplementation : IApi
{
	private static ModEntry Instance => ModEntry.Instance;

	public Upgrade ABUpgrade
		=> ABUpgradeManager.ABUpgrade;

	public bool HasABUpgrade(Type cardType)
		=> Instance.Manager.HasABUpgrade(cardType);

	public bool HasABUpgrade(Card card)
		=> Instance.Manager.HasABUpgrade(card);

	public void RegisterABUpgrade(Type cardType, Upgrade upgradeToCopyDataFrom, Func<State, Combat, Card, List<CardAction>> actions)
		=> RegisterABUpgrade(cardType, (s, card) =>
		{
			var oldUpgrade = card.upgrade;
			card.upgrade = upgradeToCopyDataFrom;
			var result = card.GetData(s);
			card.upgrade = oldUpgrade;
			return result;
		}, actions);

	public void RegisterABUpgrade(Type cardType, Func<State, Card, CardData> data, Upgrade upgradeToCopyActionsFrom)
		=> RegisterABUpgrade(cardType, data, (s, c, card) =>
		{
			var oldUpgrade = card.upgrade;
			card.upgrade = upgradeToCopyActionsFrom;
			var result = card.GetActions(s, c);
			card.upgrade = oldUpgrade;
			return result;
		});

	public void RegisterABUpgrade(Type cardType, Func<State, Card, CardData> data, Func<State, Combat, Card, List<CardAction>> actions)
		=> Instance.Manager.RegisterABUpgrade(cardType, data, actions);
}