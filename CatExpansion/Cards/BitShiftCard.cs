using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class BitShiftCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/BitShift.png"), StableSpr.cards_ShiftShot).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BitShift", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, flippable = true },
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 1 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new EnemyMoveAction { dir = -1 },
				new AMove { targetPlayer = true, dir = 2 },
			],
			Upgrade.B => [
				new AMove { targetPlayer = true, dir = -1 },
				new EnemyMoveAction { dir = -2 },
				new AMove { targetPlayer = true, dir = 4 },
			],
			_ => [
				new EnemyMoveAction { dir = -1 },
				new AMove { targetPlayer = true, dir = 2 },
			],
		};
}