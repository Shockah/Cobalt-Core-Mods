using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
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

	private int GetFrogproofing()
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 3,
			_ => 1,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = 0;
		data.retain = true;
		data.exhaust = upgrade == Upgrade.B;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => new()
			{
				new ADrawCard
				{
					count = 2
				},
				new AStatus
				{
					status = (Status)Instance.FrogproofingStatus.Id!.Value,
					statusAmount = GetFrogproofing(),
					targetPlayer = true
				},
				new ADummyAction(),
				new ADummyAction()
			},
			_ => new()
			{
				new AStatus
				{
					status = (Status)Instance.FrogproofingStatus.Id!.Value,
					statusAmount = GetFrogproofing(),
					targetPlayer = true
				},
				new ADummyAction(),
				new ADummyAction()
			}
		};
}
