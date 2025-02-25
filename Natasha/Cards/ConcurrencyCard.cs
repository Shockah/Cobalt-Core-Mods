﻿using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class ConcurrencyCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Concurrency.png"), StableSpr.cards_MultiShot).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Concurrency", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, floppable = true },
			_ => new() { cost = 2 }
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 1), disabled = flipped },
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new AAttack { damage = GetDmg(s, 2) }).AsCardAction.Disabled(flipped),
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 3, 3, new AAttack { damage = GetDmg(s, 3) }).AsCardAction.Disabled(flipped),
				new ADummyAction(),
				new AEnergy { changeAmount = 1, disabled = !flipped },
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 3, new AAttack { damage = GetDmg(s, 1) }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 1, 2, new AAttack { damage = GetDmg(s, 2) }).AsCardAction,
				new AAttack { damage = GetDmg(s, 3) },
			],
			_ => [
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 3, 3, new AAttack { damage = GetDmg(s, 1) }).AsCardAction,
				ModEntry.Instance.KokoroApi.Sequence.MakeAction(uuid, IKokoroApi.IV2.ISequenceApi.Interval.Combat, 2, 2, new AAttack { damage = GetDmg(s, 2) }).AsCardAction,
				new AAttack { damage = GetDmg(s, 3) },
			]
		};
}
