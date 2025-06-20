using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Dracula;

internal sealed class AuraOfDarknessCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("AuraOfDarkness", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "AuraOfDarkness", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			recycle = upgrade != Upgrade.B,
			infinite = upgrade == Upgrade.B,
			retain = upgrade == Upgrade.B,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.Equation(
						ModEntry.Instance.KokoroApi.Conditional.Status(ModEntry.Instance.BleedingStatus.Status),
						IKokoroApi.IV2.IConditionalApi.EquationOperator.LessThanOrEqual,
						ModEntry.Instance.KokoroApi.Conditional.Constant(2),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.Possession
					),
					new ADrawCard { count = 1 }
				).AsCardAction,
				new AStatus { targetPlayer = true, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			],
			_ => [
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			]
		};
}
