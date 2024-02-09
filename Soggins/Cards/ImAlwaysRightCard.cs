using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class ImAlwaysRightCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.ImAlwaysRight",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "ImAlwaysRight.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.ImAlwaysRight",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ImAlwaysRightCardName);
		registry.RegisterCard(card);
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 2,
			_ => 4
		};

	private Status GetStatus()
		=> upgrade switch
		{
			Upgrade.A => (Status)Instance.BidingTimeStatus.Id!.Value,
			Upgrade.B => (Status)Instance.DoublersLuckStatus.Id!.Value,
			_ => (Status)Instance.BidingTimeStatus.Id!.Value
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = GetCost();
		data.exhaust = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				status = GetStatus(),
				statusAmount = 1,
				targetPlayer = true
			}
		];
}
