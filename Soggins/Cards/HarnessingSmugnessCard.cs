using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using System.Collections.Generic;

namespace Shockah.Soggins;

[CardMeta(rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
public sealed class HarnessingSmugnessCard : Card, IRegisterableCard, IFrogproofCard
{
	private static ModEntry Instance => ModEntry.Instance;

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
		data.art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top;
		data.cost = 1;
		data.floppable = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> new()
		{
			Instance.Api.MakeAddSmugAction(s, GetTopSmug(), disabled: flipped),
			new AAttack
			{
				damage = GetDmg(s, GetAttack()),
				disabled = flipped
			},
			new ADummyAction(),
			Instance.Api.MakeAddSmugAction(s, GetBottomSmug(), disabled: !flipped),
			new AStatus
			{
				status = Status.shield,
				statusAmount = GetShield(),
				targetPlayer = true,
				disabled = !flipped
			}
		};
}
