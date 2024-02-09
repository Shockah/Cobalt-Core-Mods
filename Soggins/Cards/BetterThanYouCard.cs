using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class BetterThanYouCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.BetterThanYou",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "BetterThanYou.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.BetterThanYou",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.BetterThanYouCardName);
		registry.RegisterCard(card);
	}

	private string GetText()
		=> upgrade switch
		{
			Upgrade.A => I18n.BetterThanYouCardTextA,
			Upgrade.B => I18n.BetterThanYouCardTextB,
			_ => I18n.BetterThanYouCardText0,
		};

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 0,
			Upgrade.B => 1,
			_ => 1,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.description = GetText();
		data.cost = GetCost();
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpecificColorSearch
				{
					Deck = (Deck)Instance.SogginsDeck.Id!.Value,
					Amount = 10,
					IgnoreCardID = uuid
				}
			],
			_ => [
				new ADiscardAndSearchFromADeck
				{
					Deck = (Deck)Instance.SogginsDeck.Id!.Value,
					IgnoreCardID = uuid
				}
			]
		};
}
