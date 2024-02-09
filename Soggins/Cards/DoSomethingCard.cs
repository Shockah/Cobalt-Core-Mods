using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = [Upgrade.A, Upgrade.B])]
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
			cardArt: Art,
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
			Instance.KokoroApi.Actions.MakePlayRandomCardsFromAnywhere(
				cardIds: Instance.KokoroApi.GetCardsEverywhere(
					state: s,
					hand: false,
					drawPile: upgrade != Upgrade.A,
					discardPile: upgrade != Upgrade.None,
					exhaustPile: upgrade == Upgrade.B
				).Where(c => c.Key() != Key()).Select(c => c.uuid),
				amount: upgrade == Upgrade.B ? 2 : 1
			)
		};
}
