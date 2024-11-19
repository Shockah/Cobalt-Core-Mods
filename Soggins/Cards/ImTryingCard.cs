using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class ImTryingCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.ImTrying",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "ImTrying.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.ImTrying",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ImTryingCardName);
		registry.RegisterCard(card);
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 1,
			Upgrade.B => 0,
			_ => 1,
		};

	private int GetCardsInHandAfterPlaying(Combat combat)
		=> upgrade switch
		{
			Upgrade.B => Math.Clamp(combat.hand.Count + 1, 0, 10),
			_ => Math.Clamp(combat.hand.Count - 1, 0, 10),
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = GetCost();
		data.retain = upgrade != Upgrade.None;
		data.exhaust = upgrade == Upgrade.B;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		var cardsInHandAfterPlaying = GetCardsInHandAfterPlaying(c);

		if (upgrade == Upgrade.B)
			actions.Add(new ADrawCard
			{
				count = 2
			});

		actions.Add(new AVariableHint
		{
			hand = true,
			handAmount = cardsInHandAfterPlaying
		});
		actions.Add(Instance.KokoroApi.Actions.MakeExhaustEntireHandImmediate());

		actions.Add(ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
			new AAddCard
			{
				card = new RandomPlaceholderApologyCard(),
				destination = CardDestination.Hand,
				amount = cardsInHandAfterPlaying,
				xHint = 1
			},
			new AAddApologyCard
			{
				Destination = CardDestination.Hand,
				Amount = cardsInHandAfterPlaying
			}
		).AsCardAction);

		return actions;
	}
}
