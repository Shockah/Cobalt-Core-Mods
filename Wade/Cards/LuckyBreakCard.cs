using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class LuckyBreakCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/LuckyBreak.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LuckyBreak", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, flippable = true, artTint = "ffffff" },
			Upgrade.A => new() { cost = 0, artTint = "ffffff" },
			_ => new() { cost = 1, artTint = "ffffff" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AVariableHint { status = Odds.OddsStatus.Status },
			new AMove { targetPlayer = true, dir = s.ship.Get(Odds.OddsStatus.Status), preferRightWhenZero = true, xHint = 1 },
			new Odds.RollAction(),
		];
}