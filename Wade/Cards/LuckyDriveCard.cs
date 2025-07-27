using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class LuckyDriveCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.WadeDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/LuckyDrive.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LuckyDrive", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, exhaust = true, artTint = "ffffff" },
			_ => new() { cost = 2, exhaust = true, artTint = "ffffff" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Odds.LuckyDriveStatus.Status, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Odds.LuckyDriveStatus.Status, statusAmount = 1 },
			]
		};
}