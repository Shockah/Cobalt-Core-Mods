using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SanguinePathCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("SanguinePath", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Ace,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SanguinePath", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.None ? 0 : 1,
			infinite = upgrade == Upgrade.A,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
				ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new ADiscount { CardId = uuid }).AsCardAction,
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			]
		};
}