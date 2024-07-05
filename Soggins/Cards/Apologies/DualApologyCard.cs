using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

[CardMeta(dontOffer = true, rarity = Rarity.common, unreleased = true)]
public sealed class DualApologyCard : ApologyCard, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite TopArt = null!;
	private static ExternalSprite BottomArt = null!;

	public Card? FirstCard;
	public Card? SecondCard;

	public bool CustomFlipped = false;
	public bool CustomFlopped = false;

	public override void RegisterArt(ISpriteRegistry registry)
	{
		TopArt = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.ApologyDualTop",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "ApologyDualTop.png"))
		);
		BottomArt = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.ApologyDualBottom",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "ApologyDualBottom.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.Apology.Dual",
			cardType: GetType(),
			cardArt: TopArt,
			actualDeck: ModEntry.Instance.ApologiesDeck
		);
		card.AddLocalisation(I18n.ApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		var firstData = FirstCard?.GetData(state);
		var secondData = SecondCard?.GetData(state);
		data.art = (Spr)(CustomFlopped ? BottomArt : TopArt).Id!.Value;
		data.floppable = true;
		data.flippable = firstData?.flippable == true || secondData?.flippable == true;
		data.singleUse = firstData?.singleUse == true || secondData?.singleUse == true;
		return data;
	}

	public override double GetApologyWeight(State state, Combat combat, int timesGiven)
		=> 0;

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> firstActions = FirstCard?.GetActions(s, c).Select(a => { a.disabled = CustomFlopped; return a; }).ToList() ?? [];
		List<CardAction> secondActions = SecondCard?.GetActions(s, c).Select(a => { a.disabled = !CustomFlopped; return a; }).ToList() ?? [];
		int perSide = Math.Max(firstActions.Count, secondActions.Count);

		List<CardAction> actions = [];
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
