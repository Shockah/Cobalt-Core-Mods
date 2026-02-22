using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class MoodSwingCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.MoodSwing",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "MoodSwing.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.MoodSwing",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.MoodSwingCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, retain = true },
			_ => new() { cost = 1 },
		};

	public bool IsFrogproof(State state, Combat? combat)
		=> upgrade == Upgrade.B;

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AVariableHint { status = (Status)Instance.SmugStatus.Id!.Value },
			new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = (Status)Instance.SmugStatus.Id!.Value, statusAmount = -s.ship.Get((Status)Instance.SmugStatus.Id!.Value), xHint = -1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
		];

	public int ModifySmugSwing(State state, Combat combat, int amount)
		=> 0;
}