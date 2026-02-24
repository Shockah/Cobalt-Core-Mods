using System;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class MoodSwingCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly Dictionary<int, ExternalSprite> Art = [];

	public void RegisterArt(ISpriteRegistry registry)
	{
		foreach (var smug in Enumerable.Range(-3, 7))
			Art[smug] = registry.RegisterArtOrThrow(
				id: $"{GetType().Namespace}.CardArt.MoodSwing{smug}",
				file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", $"MoodSwing{smug}.png"))
			);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.MoodSwing",
			cardType: GetType(),
			cardArt: Art[0],
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.MoodSwingCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = new CardData { art = (Spr)Art[Math.Clamp(state.ship.Get((Status)Instance.SmugStatus.Id!.Value), -3, 3)].Id!.Value };
		return upgrade switch
		{
			Upgrade.A => data with { cost = 1, retain = true },
			_ => data with { cost = 1 },
		};
	}

	public bool IsFrogproof(State state, Combat? combat)
		=> upgrade == Upgrade.B;

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AVariableHint { status = (Status)Instance.SmugStatus.Id!.Value },
			new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = (Status)Instance.SmugStatus.Id!.Value, statusAmount = s.ship.Get((Status)Instance.SmugStatus.Id!.Value) * (Instance.Api.IsCurrentlyDoubling(s, c) ? 1 : -1), xHint = -1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
			new ADummyAction(),
			new ADummyAction(),
		];

	public int ModifySmugSwing(State state, Combat combat, int amount)
		=> 0;
}