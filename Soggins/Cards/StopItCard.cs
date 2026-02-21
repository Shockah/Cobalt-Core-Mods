using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class StopItCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.StopIt",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "StopIt.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.StopIt",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.StopItCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, retain = true, exhaust = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = (Status)Instance.FrogproofingStatus.Id!.Value, statusAmount = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = (Status)Instance.SmugStatus.Id!.Value, statusAmount = 1 },
				new ADummyAction(),
				new ADummyAction()
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = (Status)Instance.FrogproofingStatus.Id!.Value, statusAmount = 3 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = (Status)Instance.SmugStatus.Id!.Value, statusAmount = 0 },
				new ADummyAction(),
				new ADummyAction()
			],
			_ => [
				new AStatus { targetPlayer = true, status = (Status)Instance.FrogproofingStatus.Id!.Value, statusAmount = 1 },
				new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = (Status)Instance.SmugStatus.Id!.Value, statusAmount = 0 },
				new ADummyAction(),
				new ADummyAction()
			]
		};
}
