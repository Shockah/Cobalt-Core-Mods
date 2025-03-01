using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Natasha;

internal sealed class DeprogramCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Deprogram", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, floppable = true, retain = true, temporary = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
			Upgrade.B => new() { cost = 1, floppable = true, exhaust = true, temporary = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
			_ => new() { cost = 1, floppable = true, temporary = true, art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = false, status = Status.rockFactory, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = true, status = Status.rockFactory, statusAmount = 1, disabled = !flipped },
			],
			_ => [
				new AStatus { targetPlayer = false, status = Reprogram.DeprogrammedStatus.Status, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new ASpawn { thing = new Asteroid(), disabled = !flipped },
			]
		};
}
