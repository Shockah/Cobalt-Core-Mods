using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class HarnessingSmugnessCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite TopArt = null!;
	private static ExternalSprite BottomArt = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		TopArt = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.HarnessingSmugnessTop",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "HarnessingSmugnessTop.png"))
		);
		BottomArt = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.HarnessingSmugnessBottom",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "HarnessingSmugnessBottom.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.HarnessingSmugness",
			cardType: GetType(),
			cardArt: ModEntry.Instance.SogginsDeckBorder,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.HarnessingSmugnessCardName);
		registry.RegisterCard(card);
	}

	private int GetTopSmug()
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 1,
			_ => 1,
		};

	private int GetBottomSmug()
		=> upgrade switch
		{
			Upgrade.A => -1,
			Upgrade.B => -2,
			_ => -1,
		};

	private int GetAttack()
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 2,
			_ => 2,
		};

	private int GetShield()
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 3,
			_ => 2,
		};

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = (Spr)(flipped ? BottomArt : TopArt).Id!.Value;
		data.cost = 1;
		data.floppable = true;
		data.infinite = true;
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
			new AAttack
			{
				damage = GetDmg(s, GetAttack()),
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
				status = Status.shield,
				statusAmount = GetShield(),
				targetPlayer = true,
				disabled = !flipped
			}
		};
}
