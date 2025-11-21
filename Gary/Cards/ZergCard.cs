using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class ZergCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Zerg.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Zerg", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Stack.JengaStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Stack.JengaStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Stack.JengaStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
			],
			_ => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
			],
		};
}