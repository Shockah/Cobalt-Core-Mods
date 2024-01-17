using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodShieldCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BloodShield", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodShield", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			floppable = true,
			infinite = upgrade == Upgrade.A
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
				statusAmount = upgrade == Upgrade.B ? 4 : 3,
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
					amount: upgrade == Upgrade.B ? 3 : 2
				),
				action: new AHeal
				{
					targetPlayer = true,
					healAmount = upgrade == Upgrade.B ? 2 : 1
				}
			).Disabled(!flipped)
		];
}
