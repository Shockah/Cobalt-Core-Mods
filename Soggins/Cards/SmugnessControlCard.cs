using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class SmugnessControlCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite TopArt = null!;
	private static ExternalSprite BottomArt = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		TopArt = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.SmugnessControlTop",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "SmugnessControlTop.png"))
		);
		BottomArt = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.SmugnessControlBottom",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "SmugnessControlBottom.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.SmugnessControl",
			cardType: GetType(),
			cardArt: TopArt,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.SmugnessControlCardName);
		registry.RegisterCard(card);
	}

	private int GetCost()
		=> upgrade switch
		{
			Upgrade.A => 0,
			Upgrade.B => 1,
			_ => 1,
		};

	private int GetTopSmug()
		=> upgrade switch
		{
			Upgrade.A => 1,
			Upgrade.B => 3,
			_ => 2,
		};

	private int GetBottomSmug()
		=> -GetTopSmug();

	private int GetTempShield()
		=> upgrade switch
		{
			Upgrade.A => 1,
			Upgrade.B => 3,
			_ => 2,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = (Spr)(flipped ? BottomArt : TopArt).Id!.Value;
		data.cost = GetCost();
		data.floppable = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			new AStatus
			{
				status = (Status)Instance.SmugStatus.Id!.Value,
				statusAmount = GetTopSmug(),
				targetPlayer = true,
				disabled = flipped
			},
			new AStatus
			{
				status = Status.tempShield,
				statusAmount = GetTempShield(),
				targetPlayer = true,
				disabled = flipped
			},
			new ADummyAction(),
			new AStatus
			{
				status = (Status)Instance.SmugStatus.Id!.Value,
				statusAmount = GetBottomSmug(),
				targetPlayer = true,
				disabled = !flipped
			},
			new AStatus
			{
				status = Status.tempShield,
				statusAmount = GetTempShield(),
				targetPlayer = true,
				disabled = !flipped
			}
		};
}
