﻿using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class SafetyFirstCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/SafetyFirst.png"), StableSpr.cards_ShieldSurge).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SafetyFirst", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1 },
			Upgrade.B => new() { cost = 2 },
			_ => new() { cost = 2 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			],
		};
}