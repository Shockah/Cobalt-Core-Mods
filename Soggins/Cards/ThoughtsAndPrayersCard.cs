using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class ThoughtsAndPrayersCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	private static bool IsDuringTryPlayCard = false;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.ThoughtsAndPrayers",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "ThoughtsAndPrayers.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.ThoughtsAndPrayers",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ThoughtsAndPrayersCardName);
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

	private int GetAmount()
		=> upgrade switch
		{
			Upgrade.A => 4,
			Upgrade.B => 3,
			_ => 3,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = upgrade == Upgrade.B ? 1 : 2;
		data.exhaust = upgrade == Upgrade.B;
		data.buoyant = upgrade == Upgrade.B;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = new()
		{
			new AStatus
			{
				status = (Status)Instance.SmugStatus.Id!.Value,
				statusAmount = 1,
				targetPlayer = true
			}
		};

		if (IsDuringTryPlayCard)
		{
			for (int i = 0; i < GetAmount(); i++)
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
				amount = GetAmount()
			});
		}

		for (int i = 0; i < 2; i++)
			actions.Add(new ADummyAction());

		return actions;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}
