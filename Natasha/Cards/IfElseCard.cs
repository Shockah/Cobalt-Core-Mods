using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class IfElseCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.NatashaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/IfElse.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "IfElse", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 3);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> (HashSet<ICardTraitEntry>)(upgrade switch
		{
			Upgrade.B => [Limited.Trait],
			_ => [],
		});

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 2 }
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			ModEntry.Instance.KokoroApi.Conditional.MakeAction(
				ModEntry.Instance.KokoroApi.Conditional.Equation(
					ModEntry.Instance.KokoroApi.Conditional.Status(Status.evade),
					IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal,
					ModEntry.Instance.KokoroApi.Conditional.Constant(0),
					IKokoroApi.IV2.IConditionalApi.EquationStyle.Possession
				).SetHideOperator(true),
				new OneLinerAction
				{
					Actions = [
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = upgrade == Upgrade.A ? 3 : 2 },
						ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Stop, out var evadeStop).AsCardAction,
					]
				}
			).AsCardAction.Disabled(s != DB.fakeState && s.ship.Get(Status.evade) > 0),
			ModEntry.Instance.KokoroApi.ContinueStop.MakeFlaggedAction(
				IKokoroApi.IV2.IContinueStopApi.ActionType.Stop,
				evadeStop,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.Equation(
						ModEntry.Instance.KokoroApi.Conditional.Status(Status.shield),
						IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal,
						ModEntry.Instance.KokoroApi.Conditional.Constant(0),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.Possession
					).SetHideOperator(true),
					new OneLinerAction
					{
						Actions = [
							new AStatus { targetPlayer = true, status = Status.shield, statusAmount = upgrade == Upgrade.A ? 4 : 3 },
							ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Stop, out var shieldStop).AsCardAction,
						]
					}
				).AsCardAction
			).AsCardAction.Disabled(s != DB.fakeState && s.ship.Get(Status.evade) == 0 || s.ship.Get(Status.shield) > 0),
			ModEntry.Instance.KokoroApi.ContinueStop.MakeFlaggedAction(
				IKokoroApi.IV2.IContinueStopApi.ActionType.Stop,
				[evadeStop, shieldStop],
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = upgrade == Upgrade.A ? 4 : 3 }
			).AsCardAction.Disabled(s != DB.fakeState && (s.ship.Get(Status.evade) == 0 || s.ship.Get(Status.shield) == 0)),
		];
}
