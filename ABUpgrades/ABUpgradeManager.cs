using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ABUpgrades;

internal sealed class ABUpgradeManager
{
	internal const Upgrade ABUpgrade = (Upgrade)21370001;

	private readonly Dictionary<Type, (Func<State, Card, CardData> Data, Func<State, Combat, Card, List<CardAction>> Actions)> RegisteredUpgrades = new();

	internal void ApplyMetaChange(Type cardType)
	{
		if (DB.cardMetas is null)
			return;
		if (Activator.CreateInstance(cardType) is not Card card)
			return;
		ApplyMetaChange(card.Key(), cardType);
	}

	internal void ApplyMetaChange(string key, Type cardType)
	{
		if (DB.cardMetas is null)
			return;
		if (!DB.cardMetas.TryGetValue(key, out var meta))
			return;

		var hasABUpgrade = HasABUpgrade(cardType);
		var containsABUpgrade = meta.upgradesTo.Contains(ABUpgrade);
		if (hasABUpgrade != containsABUpgrade)
		{
			if (hasABUpgrade)
				meta.upgradesTo = meta.upgradesTo.Append(ABUpgrade).ToArray();
			else
				meta.upgradesTo = meta.upgradesTo.Where(u => u != ABUpgrade).ToArray();
		}
	}

	public bool HasABUpgrade(Type cardType)
		=> RegisteredUpgrades.ContainsKey(cardType);

	public bool HasABUpgrade(Card card)
		=> HasABUpgrade(card.GetType());

	public void RegisterABUpgrade(Type cardType, Func<State, Card, CardData> data, Func<State, Combat, Card, List<CardAction>> actions)
	{
		RegisteredUpgrades[cardType] = (Data: data, Actions: actions);
		ApplyMetaChange(cardType);
	}

	public CardData? GetABUpgradeData(State state, Card card)
		=> RegisteredUpgrades.TryGetValue(card.GetType(), out var entry) ? entry.Data(state, card) : null;

	public List<CardAction>? GetABUpgradeActions(State state, Combat combat, Card card)
		=> RegisteredUpgrades.TryGetValue(card.GetType(), out var entry) ? entry.Actions(state, combat, card) : null;
}