using System;
using System.Collections.Generic;

namespace Shockah.ABUpgrades;

public interface IApi
{
	Upgrade ABUpgrade { get; }

	bool HasABUpgrade(Type cardType);
	bool HasABUpgrade(Card card);

	void RegisterABUpgrade(Type cardType, Upgrade upgradeToCopyDataFrom, Func<State, Combat, Card, List<CardAction>> actions);
	void RegisterABUpgrade(Type cardType, Func<State, Card, CardData> data, Upgrade upgradeToCopyActionsFrom);
	void RegisterABUpgrade(Type cardType, Func<State, Card, CardData> data, Func<State, Combat, Card, List<CardAction>> actions);
}