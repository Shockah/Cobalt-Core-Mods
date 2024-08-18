using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class HarnessingSmugnessCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite TopArt = null!;
	private static ExternalSprite BottomArt = null!;
	private static ExternalSprite Top23Art = null!;
	private static ExternalSprite Bottom23Art = null!;

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
		Top23Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.HarnessingSmugness23Top",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "HarnessingSmugness23Top.png"))
		);
		Bottom23Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.HarnessingSmugness23Bottom",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "HarnessingSmugness23Bottom.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.HarnessingSmugness",
			cardType: GetType(),
			cardArt: TopArt,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.HarnessingSmugnessCardName);
		registry.RegisterCard(card);
	}

	private ExternalSprite GetArt()
	{
		if (upgrade == Upgrade.A)
			return flipped ? Bottom23Art : Top23Art;
		return this.flipped ? BottomArt : TopArt;
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.art = (Spr)GetArt().Id!.Value;
		data.cost = 1;
		data.floppable = true;
		data.infinite = upgrade != Upgrade.B;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = 2,
					targetPlayer = true,
					disabled = flipped
				},
				new AAttack
				{
					damage = GetDmg(s, 2),
					disabled = flipped
				},
				new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = -2,
					targetPlayer = true,
					disabled = !flipped
				},
				new AStatus
				{
					status = Status.shield,
					statusAmount = 1,
					targetPlayer = true,
					disabled = !flipped
				},
				new AStatus
				{
					status = Status.tempShield,
					statusAmount = 1,
					targetPlayer = true,
					disabled = !flipped
				}
			],
			Upgrade.B => [
				new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = 3,
					targetPlayer = true,
					disabled = flipped
				},
				new AAttack
				{
					damage = GetDmg(s, 3),
					disabled = flipped
				},
				new ADummyAction(),
				new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = -3,
					targetPlayer = true,
					disabled = !flipped
				},
				new AStatus
				{
					status = Status.shield,
					statusAmount = 3,
					targetPlayer = true,
					disabled = !flipped
				}
			],
			_ => [
				new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = 1,
					targetPlayer = true,
					disabled = flipped
				},
				new AAttack
				{
					damage = GetDmg(s, 1),
					disabled = flipped
				},
				new ADummyAction(),
				new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = -1,
					targetPlayer = true,
					disabled = !flipped
				},
				new AStatus
				{
					status = Status.shield,
					statusAmount = 1,
					targetPlayer = true,
					disabled = !flipped
				}
			]
		};
}
