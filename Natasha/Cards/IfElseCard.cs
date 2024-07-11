using Nanoray.PluginManager;
using Nickel;
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

		Limited.SetDefaultLimitedUses(entry.UniqueName, Upgrade.B, 3);
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
			ModEntry.Instance.KokoroApi.ConditionalActions.Make(
				ModEntry.Instance.KokoroApi.ConditionalActions.Equation(
					ModEntry.Instance.KokoroApi.ConditionalActions.Status(Status.evade),
					IKokoroApi.IConditionalActionApi.EquationOperator.Equal,
					ModEntry.Instance.KokoroApi.ConditionalActions.Constant(0),
					IKokoroApi.IConditionalActionApi.EquationStyle.Possession,
					hideOperator: true
				),
				new OneLinerAction
				{
					Actions = [
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = upgrade == Upgrade.A ? 3 : 2 },
						ModEntry.Instance.KokoroApi.Actions.MakeStop(out var evadeStop),
					]
				}
			).Disabled(s.ship.Get(Status.evade) > 0),
			ModEntry.Instance.KokoroApi.Actions.MakeStopped(
				evadeStop,
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					ModEntry.Instance.KokoroApi.ConditionalActions.Equation(
						ModEntry.Instance.KokoroApi.ConditionalActions.Status(Status.shield),
						IKokoroApi.IConditionalActionApi.EquationOperator.Equal,
						ModEntry.Instance.KokoroApi.ConditionalActions.Constant(0),
						IKokoroApi.IConditionalActionApi.EquationStyle.Possession,
						hideOperator: true
					),
					new OneLinerAction
					{
						Actions = [
							new AStatus { targetPlayer = true, status = Status.shield, statusAmount = upgrade == Upgrade.A ? 4 : 3 },
							ModEntry.Instance.KokoroApi.Actions.MakeStop(out var shieldStop),
						]
					}
				)
			).Disabled(s.ship.Get(Status.evade) == 0 || s.ship.Get(Status.shield) > 0),
			ModEntry.Instance.KokoroApi.Actions.MakeStopped(
				evadeStop,
				ModEntry.Instance.KokoroApi.Actions.MakeStopped(
					shieldStop,
					new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = upgrade == Upgrade.A ? 4 : 3 }
				)
			).Disabled(s.ship.Get(Status.evade) == 0 || s.ship.Get(Status.shield) == 0)
		];
}
