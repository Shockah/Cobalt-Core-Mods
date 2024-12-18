﻿using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class PsychicDamageCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PsychicDamage.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PsychicDamage", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AAttack { damage = GetDmg(s, 2) }).AsCardAction,
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AAttack { damage = GetDmg(s, 2) }).AsCardAction,
			],
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 2) },
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AAttack { damage = GetDmg(s, 3) }).AsCardAction,
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1) },
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AAttack { damage = GetDmg(s, 2) }).AsCardAction,
			]
		};
}
