using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class BloodShieldCard : Card, IDraculaCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BloodShield", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodShield", "name"]).Localize
		});
	}

	private int Shield
		=> upgrade switch
		{
			Upgrade.A => 4,
			Upgrade.B => 3,
			_ => 3,
		};

	private int ShieldCost
		=> upgrade switch
		{
			Upgrade.A => 2,
			Upgrade.B => 3,
			_ => 3,
		};

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			floppable = true,
			infinite = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AHurt
			{
				targetPlayer = true,
				hurtAmount = 1,
				disabled = flipped
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.shield,
				statusAmount = Shield,
				disabled = flipped
			},
			new ADummyAction(),
			ModEntry.Instance.KokoroApi.ActionCosts.Make(
				cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
					resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
						status: Status.shield,
						costUnsatisfiedIcon: ModEntry.Instance.ShieldCostOff.Sprite,
						costSatisfiedIcon: ModEntry.Instance.ShieldCostOn.Sprite
					),
					amount: ShieldCost
				),
				action: new AHeal
				{
					targetPlayer = true,
					healAmount = 1
				}
			).Disabled(!flipped)
		];
}
