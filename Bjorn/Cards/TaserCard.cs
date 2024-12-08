using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class TaserCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Taser.png"), StableSpr.cards_StunCharge).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Taser", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 1 },
			a: () => new() { cost = 1, retain = true },
			b: () => new() { cost = 1 }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new AnalyzedCondition { Analyzed = ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, this, Analyze.AnalyzedTrait) },
					new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 1 }
				).AsCardAction,
				new AAttack { damage = GetDmg(s, 1) }
			],
			_ => [
				new AnalyzeCostAction { CardId = uuid, Action = new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 1 } },
				new AAttack { damage = GetDmg(s, 1) }
			],
		};
}
