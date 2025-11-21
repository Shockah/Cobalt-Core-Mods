using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class BigGunsCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/BigGuns.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BigGuns", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.None, 3);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.A, 3);
	}

	public override CardData GetData(State state)
		=> new() { cost = 0 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry>(),
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Finite.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new AttackDrone { targetPlayer = false, upgraded = true } },
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new AttackDrone { targetPlayer = false, upgraded = true } },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 },
			],
			_ => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false, upgraded = true } },
				new AStatus { targetPlayer = true, status = Status.energyLessNextTurn, statusAmount = 1 },
			],
		};
}