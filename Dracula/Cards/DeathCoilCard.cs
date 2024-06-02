using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DeathCoilCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = StableSpr.cards_HandCannon,
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
					action: new AAttack { damage = GetDmg(s, 2) }
				),
				new AHeal
				{
					targetPlayer = true,
					healAmount = 2,
					canRunAfterKill = true
				},
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			],
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 2) },
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: new HullCondition { BelowHalf = true },
					action: new AHeal
					{
						targetPlayer = true,
						healAmount = 2,
						canRunAfterKill = true
					}
				).CanRunAfterKill(),
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			],
			_ => [
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: new HullCondition { BelowHalf = false },
					action: new AAttack { damage = GetDmg(s, 2) }
				),
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: new HullCondition { BelowHalf = true },
					action: new AHeal
					{
						targetPlayer = true,
						healAmount = 2,
						canRunAfterKill = true
					}
				).CanRunAfterKill(),
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			]
		};

	private sealed class HullCondition : IKokoroApi.IConditionalActionApi.IBoolExpression
	{
		public required bool BelowHalf;

		public bool GetValue(State state, Combat combat)
			=> BelowHalf ? state.ship.hull <= state.ship.hullMax / 2 : state.ship.hull > state.ship.hullMax / 2;

		public string GetTooltipDescription(State state, Combat? combat)
		{
			if (state.IsOutsideRun() || state == DB.fakeState)
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

		public IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat? combat, Tooltip defaultTooltip, string defaultTooltipDescription)
			=> [
				new GlossaryTooltip($"AConditional::{ModEntry.Instance.Package.Manifest.UniqueName}::HullCondition::BelowHalf={BelowHalf}")
				{
					Icon = (BelowHalf ? ModEntry.Instance.HullBelowHalf : ModEntry.Instance.HullAboveHalf).Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["condition", "hull", BelowHalf ? "below" : "above", "title"]),
					Description = defaultTooltipDescription,
				}
			];
	}
}
