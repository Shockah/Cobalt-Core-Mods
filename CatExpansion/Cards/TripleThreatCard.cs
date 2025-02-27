using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class TripleThreatCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/TripleThreat.png"), StableSpr.cards_Overdrive).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "TripleThreat", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, exhaust = true },
			Upgrade.B => new() { cost = 2, exhaust = true },
			_ => new() { cost = 1, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.libra, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.hermes, statusAmount = 1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.libra, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.hermes, statusAmount = 2 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.overdrive, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.libra, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.hermes, statusAmount = 1 },
			],
		};
}