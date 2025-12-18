using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dracula", "DeathCoil", "name"]).Localize
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
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = false },
					new AAttack { damage = GetDmg(s, 3) }
				).SetShowQuestionMark(false).AsCardAction,
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
				new AAttack { damage = GetDmg(s, 3) },
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = true },
					new AHeal
					{
						targetPlayer = true,
						healAmount = 2,
						canRunAfterKill = true
					}
				).SetShowQuestionMark(false).AsCardAction.CanRunAfterKill(),
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			],
			_ => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = false },
					new AAttack { damage = GetDmg(s, 3) }
				).SetShowQuestionMark(false).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new HullCondition { BelowHalf = true },
					new AHeal
					{
						targetPlayer = true,
						healAmount = 2,
						canRunAfterKill = true
					}
				).SetShowQuestionMark(false).AsCardAction.CanRunAfterKill(),
				new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1
				}
			]
		};
}
