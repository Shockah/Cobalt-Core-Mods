using System;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class StackCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Stack.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Stack", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var data = new CardData
		{
			art = Enchanted.GetCardArt(this),
			artTint = "ffffff",
		};
		return upgrade switch
		{
			Upgrade.A => data with { cost = 1 },
			Upgrade.B => data with { cost = 0 },
			_ => data with { cost = 1 },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AVariableHint { status = Status.shield },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = s.ship.Get(Status.shield), xHint = 1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 },
				new AVariableHint { status = Status.shield },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = Math.Min(s.ship.Get(Status.shield) + 1, s.ship.GetMaxShield()), xHint = 1 },
			],
			_ => [
				new AVariableHint { status = Status.shield },
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = s.ship.Get(Status.shield), xHint = 1 },
			],
		};
}