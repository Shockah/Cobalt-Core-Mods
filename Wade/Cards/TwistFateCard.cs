using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class TwistFateCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/TwistFate.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "TwistFate", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1, retain = true, artTint = "ffffff" },
			Upgrade.A => new() { cost = 0, artTint = "ffffff" },
			_ => new() { cost = 1, artTint = "ffffff" },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 1 },
			ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
				new Odds.OddsVariableHint(),
				new Odds.OddsVariableHint()
			).AsCardAction,
			ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
				new AStatus { targetPlayer = true, status = Odds.OddsStatus.Status, statusAmount = ModEntry.Instance.Api.GetKnownOdds(s, c) is null ? 0 : -s.ship.Get(Odds.OddsStatus.Status) * 2, xHint = -2 },
				new AStatus { targetPlayer = true, status = Odds.OddsStatus.Status, statusAmount = -s.ship.Get(Odds.OddsStatus.Status) * 2, xHint = -2 }
			).AsCardAction,
		];
}