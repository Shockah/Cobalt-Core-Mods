using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class TrendSettingCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/TrendSetting.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "TrendSetting", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, buoyant = true, exhaust = true, artTint = "ffffff" },
			_ => new() { cost = 1, buoyant = true, exhaust = true, artTint = "ffffff" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Odds.RedTrendStatus.Status, statusAmount = 2 },
				new Odds.RollAction(),
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Odds.RedTrendStatus.Status, statusAmount = 1 },
				new Odds.RollAction(),
			],
		};
}