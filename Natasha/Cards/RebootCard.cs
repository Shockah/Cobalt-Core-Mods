using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class RebootCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Reboot.png"), StableSpr.cards_Inverter).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Reboot", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 2);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [Limited.Trait],
			_ => [],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, exhaust = true, retain = true },
			Upgrade.B => new() { cost = 0 },
			_ => new() { cost = 0, exhaust = true }
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AVariableHint { hand = true, handAmount = Math.Max(c.hand.Count - 1, 0) },
			new AStatus { targetPlayer = true, status = Status.drawNextTurn, statusAmount = Math.Max(c.hand.Count - 1, 0), xHint = 1 },
			ModEntry.Instance.KokoroApi.Actions.MakeEnergyX(),
			new AStatus { targetPlayer = true, status = Status.energyNextTurn, statusAmount = c.energy - GetDataWithOverrides(s).cost, xHint = 1 },
			new AEndTurn()
		];
}
