using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DeathCoilCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("DeathCoil", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DeathCoil", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: new HullCondition { BelowHalf = false },
					action: new AAttack
					{
						damage = GetDmg(s, 2)
					}
				),
				new AHeal
				{
					targetPlayer = true,
					healAmount = 2
				},
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			],
			Upgrade.B => [
				new AAttack
				{
					damage = GetDmg(s, 2)
				},
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: new HullCondition { BelowHalf = true },
					action: new AHeal
					{
						targetPlayer = true,
						healAmount = 2
					}
				),
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			],
			_ => [
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: new HullCondition { BelowHalf = false },
					action: new AAttack
					{
						damage = GetDmg(s, 2)
					}
				),
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: new HullCondition { BelowHalf = true },
					action: new AHeal
					{
						targetPlayer = true,
						healAmount = 2
					}
				),
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			]
		};

	private sealed class HullCondition : IKokoroApi.IConditionalActionApi.IBoolExpression
	{
		[JsonProperty]
		public required bool BelowHalf { get; set; }

		public bool GetValue(State state, Combat combat)
			=> BelowHalf ? state.ship.hull <= state.ship.hullMax / 2 : state.ship.hull > state.ship.hullMax / 2;

		public string GetTooltipDescription(State state, Combat? combat)
		{
			if (state.IsOutsideRun())
				return ModEntry.Instance.Localizations.Localize(["condition", "hull", BelowHalf ? "below" : "above", "stateless"]);
			else
				return ModEntry.Instance.Localizations.Localize(["condition", "hull", BelowHalf ? "below" : "above", "stateful"], new { Hull = state.ship.hullMax / 2 });
		}

		public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
		{
			if (!dontRender)
				Draw.Sprite(
					(BelowHalf ? ModEntry.Instance.HullBelowHalf : ModEntry.Instance.HullAboveHalf).Sprite,
					position.x,
					position.y,
					color: isDisabled ? Colors.disabledIconTint : Colors.white
				);
			position.x += 8;
		}
	}
}
