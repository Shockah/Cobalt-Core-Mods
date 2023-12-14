using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class ImTryingCard : Card, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	private static bool IsDuringTryPlayCard = false;

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

	public void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Finalizer))
		);
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
			Upgrade.B => Math.Min(combat.hand.Count + 1, 10),
			_ => combat.hand.Count - 1,
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
		List<CardAction> actions = new();
		int cardsInHandAfterPlaying = GetCardsInHandAfterPlaying(c);

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
		actions.Add(new AFixedExhaustEntireHand());

		if (IsDuringTryPlayCard)
		{
			for (int i = 0; i < cardsInHandAfterPlaying; i++)
				actions.Add(new AAddCard
				{
					card = SmugStatusManager.GenerateAndTrackApology(s, c, s.rngActions),
					destination = CardDestination.Hand,
					omitFromTooltips = i != 0
				});
		}
		else
		{
			actions.Add(new AAddCard
			{
				card = new RandomPlaceholderApologyCard(),
				destination = CardDestination.Hand,
				amount = cardsInHandAfterPlaying,
				xHint = 1
			});
		}

		return actions;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}
