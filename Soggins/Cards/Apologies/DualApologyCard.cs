using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

[CardMeta(dontOffer = true, rarity = Rarity.common, unreleased = true)]
public sealed class DualApologyCard : ApologyCard, IRegisterableCard
{
	public Card? FirstCard;
	public Card? SecondCard;

	public bool CustomFlipped = false;
	public bool CustomFlopped = false;

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Dual",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		var firstData = FirstCard?.GetData(state);
		var secondData = SecondCard?.GetData(state);
		data.art = CustomFlopped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top;
		data.floppable = true;
		data.flippable = firstData?.flippable == true || secondData?.flippable == true;
		data.singleUse = firstData?.singleUse == true || secondData?.singleUse == true;
		return data;
	}

	public override double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> 0;

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> firstActions = FirstCard?.GetActions(s, c).Select(a => { a.disabled = CustomFlopped; return a; }).ToList() ?? new();
		List<CardAction> secondActions = SecondCard?.GetActions(s, c).Select(a => { a.disabled = !CustomFlopped; return a; }).ToList() ?? new();
		int perSide = Math.Max(firstActions.Count, secondActions.Count);

		List<CardAction> actions = new();
		for (int i = 0; i < perSide - firstActions.Count; i++)
			actions.Add(new ADummyAction());
		actions.AddRange(firstActions);
		actions.Add(new ADummyAction());
		actions.AddRange(secondActions);
		for (int i = 0; i < perSide - secondActions.Count; i++)
			actions.Add(new ADummyAction());
		return actions;
	}

	public override void OnFlip(G g)
	{
		CustomFlopped = !CustomFlopped;
		if (FirstCard?.GetData(g.state).flippable != true && SecondCard?.GetData(g.state).flippable != true)
			return;

		if (!CustomFlopped)
			CustomFlipped = !CustomFlipped;
		flipped = CustomFlipped;
	}
}
