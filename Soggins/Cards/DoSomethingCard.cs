using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class DoSomethingCard : Card, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.DoSomething",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "DoSomething.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.DoSomething",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.DoSomethingCardName);
		registry.RegisterCard(card);
	}

	private string GetText()
		=> upgrade switch
		{
			Upgrade.A => I18n.DoSomethingCardTextA,
			Upgrade.B => I18n.DoSomethingCardTextB,
			_ => I18n.DoSomethingCardText0,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = (Spr)Art.Id!.Value;
		data.description = GetText();
		data.cost = 2;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new ADelay
			{
				time = -0.5
			},
			new APlayRandomCardsFromAnywhere
			{
				Amount = upgrade == Upgrade.B ? 2 : 1,
				FromDrawPile = upgrade != Upgrade.A,
				FromDiscard = upgrade != Upgrade.None,
				FromExhaust = upgrade == Upgrade.B,
				IgnoreCardType = Key()
			}
		};
}
