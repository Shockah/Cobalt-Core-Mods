using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class HealApologyCard : ApologyCard, IRegisterableCard
{
	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Heal",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.ApologiesDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.singleUse = true;
		return data;
	}

	public override double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> timesGiven > 0 ? 0 : base.GetApologyWeight(state, combat, timesGiven) * 0.5;

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AHullMax { targetPlayer = true, amount = 1 },
				new AHeal { targetPlayer = true, healAmount = 1 },
			],
			Upgrade.A => [
				new AHeal { targetPlayer = true, healAmount = 2 },
			],
			_ => [
				new AHeal { targetPlayer = true, healAmount = 1 },
			]
		};
}