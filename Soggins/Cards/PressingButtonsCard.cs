using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.common, upgradesTo = [Upgrade.A, Upgrade.B])]
public sealed class PressingButtonsCard : Card, IRegisterableCard
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Art = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Art = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.CardArt.PressingButtons",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CardArt", "PressingButtons.png"))
		);
	}

	public void RegisterCard(ICardRegistry registry)
	{
		ExternalCard card = new(
			globalName: $"{GetType().Namespace}.Card.PressingButtons",
			cardType: GetType(),
			cardArt: Art,
			actualDeck: ModEntry.Instance.SogginsDeck
		);
		card.AddLocalisation(I18n.PressingButtonsCardName);
		registry.RegisterCard(card);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.cost = 1;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => new()
			{
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 3,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 2)
				}
			},
			Upgrade.B => new()
			{
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 2,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 2,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 1)
				}
			},
			_ => new()
			{
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AMove
				{
					dir = 2,
					targetPlayer = true,
					isRandom = true
				},
				new AAttack
				{
					damage = GetDmg(s, 1)
				}
			}
		};
}
