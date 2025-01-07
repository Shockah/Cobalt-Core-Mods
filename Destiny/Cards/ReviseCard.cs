using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class ReviseCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DestinyDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Revise.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Revise", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0 },
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 1 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AVariableHint { status = Status.shard },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = s.ship.Get(Status.shard), xHint = 1 },
				new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = s.ship.Get(Status.shard), xHint = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.shard, statusAmount = 0 },
			],
			Upgrade.B => [
				new AVariableHint { status = Status.shard },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = s.ship.Get(Status.shard), xHint = 1 },
			],
			_ => [
				new AVariableHint { status = Status.shard },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = s.ship.Get(Status.shard), xHint = 1 },
				new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = s.ship.Get(Status.shard), xHint = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = Status.shard, statusAmount = 0 },
			],
		};
}