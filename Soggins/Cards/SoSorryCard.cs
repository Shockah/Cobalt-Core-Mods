using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class SoSorryCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.SoSorry",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "SoSorry.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.SoSorry",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.SoSorryCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = 2;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [
			new AVariableHint
			{
				status = (Status)Instance.BotchesStatus.Id!.Value
			}
		];

		var amount = Instance.Api.GetTimesBotchedThisCombat(s, c);
		if (upgrade == Upgrade.B)
			amount *= 2;

		actions.Add(ModEntry.Instance.KokoroApi.Actions.MakeSpoofed(
			renderAction: new AAddCard
			{
				card = new RandomPlaceholderApologyCard(),
				destination = CardDestination.Hand,
				amount = amount,
				xHint = 1
			},
			realAction: new AAddApologyCard
			{
				Destination = CardDestination.Hand,
				Amount = amount
			}
		));

		if (upgrade == Upgrade.A)
			actions.Add(ModEntry.Instance.KokoroApi.Actions.MakeSpoofed(
				renderAction: new AAddCard
				{
					card = new RandomPlaceholderApologyCard(),
					destination = CardDestination.Hand,
					amount = 2,
					omitFromTooltips = true
				},
				realAction: new AAddApologyCard
				{
					Destination = CardDestination.Hand,
					Amount = 2,
					omitFromTooltips = true
				}
			));

		while (actions.Count < 5)
			actions.Add(new ADummyAction());

		return actions;
	}
}
