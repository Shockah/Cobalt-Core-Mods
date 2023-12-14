using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class RunningInCirclesCard : Card, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.RunningInCircles",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "RunningInCircles.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.RunningInCircles",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.RunningInCirclesCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = upgrade == Upgrade.B ? 1 : 0;
		data.infinite = upgrade == Upgrade.B;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = new()
		{
			new AMove
			{
				dir = 1,
				targetPlayer = true,
				isRandom = true
			},
			new AMove
			{
				dir = 1,
				targetPlayer = true,
				isRandom = true
			},
			new AMove
			{
				dir = 1,
				targetPlayer = true,
				isRandom = true
			},
			new AStatus
			{
				status = Status.hermes,
				statusAmount = 1,
				targetPlayer = true
			}
		};
		if (upgrade == Upgrade.A)
			actions.Insert(0, new AStatus
			{
				status = Status.evade,
				statusAmount = 1,
				targetPlayer = true
			});
		return actions;
	}
}
