using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class SoSorryCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	private static bool IsDuringTryPlayCard = false;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.SoSorry",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "SoSorry.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.SoSorry",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.SoSorryCardName);
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

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = 2;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = new()
		{
			new AVariableHint
			{
				status = (Status)Instance.BotchesStatus.Id!.Value
			}
		};

		var amount = Instance.Api.GetTimesBotchedThisCombat(s, c);
		if (upgrade == Upgrade.B)
			amount *= 2;

		if (IsDuringTryPlayCard)
		{
			for (int i = 0; i < amount; i++)
				actions.Add(new AAddCard
				{
					card = SmugStatusManager.GenerateAndTrackApology(s, c, s.rngActions),
					destination = CardDestination.Hand,
					omitFromTooltips = i != 0
				});

			if (upgrade == Upgrade.A)
				for (int i = 0; i < 2; i++)
					actions.Add(new AAddCard
					{
						card = SmugStatusManager.GenerateAndTrackApology(s, c, s.rngActions),
						destination = CardDestination.Hand,
						omitFromTooltips = amount != 0 || i != 0
					});
		}
		else
		{
			actions.Add(new AAddCard
			{
				card = new RandomPlaceholderApologyCard(),
				destination = CardDestination.Hand,
				amount = amount,
				xHint = upgrade == Upgrade.B ? 2 : 1
			});

			if (upgrade == Upgrade.A)
				actions.Add(new AAddCard
				{
					card = new RandomPlaceholderApologyCard(),
					destination = CardDestination.Hand,
					amount = 2,
					omitFromTooltips = true
				});
		}

		while (actions.Count < 5)
			actions.Add(new ADummyAction());

		return actions;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}
