﻿using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class SpywareCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Spyware.png"), StableSpr.cards_Shield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Spyware", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 3);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 4);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 3);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { Limited.Trait };

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new LimitedUsesVariableHint { CardId = uuid },
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = this.GetLimitedUses(s), xHint = 1 },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 2 },
			],
			_ => [
				new LimitedUsesVariableHint { CardId = uuid },
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = this.GetLimitedUses(s), xHint = 1 },
			]
		};
}
