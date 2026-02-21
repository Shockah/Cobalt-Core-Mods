using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.rare, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class ExtraApologyCard : Card, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.ExtraApology",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "ExtraApology.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.ExtraApology",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.ExtraApologyCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = upgrade == Upgrade.A ? 2 : 3;
		data.exhaust = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				status = upgrade == Upgrade.B ? (Status)Instance.ConstantApologiesStatus.Id!.Value : (Status)Instance.ExtraApologiesStatus.Id!.Value,
				statusAmount = 1,
				targetPlayer = true
			},
			new AAddApologyCard(),
		];
}
