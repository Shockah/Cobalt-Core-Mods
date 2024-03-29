﻿using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class AuraOfDarknessCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("AuraOfDarkness", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "AuraOfDarkness", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			recycle = upgrade != Upgrade.B,
			infinite = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [
			new AStatus
			{
				targetPlayer = false,
				status = ModEntry.Instance.BleedingStatus.Status,
				statusAmount = 1
			},
			new AStatus
			{
				targetPlayer = true,
				status = ModEntry.Instance.BleedingStatus.Status,
				statusAmount = 1
			}
		];
		if (upgrade == Upgrade.A)
			actions.Add(new AStatus
			{
				targetPlayer = true,
				status = Status.drawNextTurn,
				statusAmount = 1
			});

		return actions;
	}
}
