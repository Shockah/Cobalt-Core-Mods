using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class ReprogramCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Reprogram.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Reprogram", "name"]).Localize
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
			Upgrade.B => new() { cost = 0 },
			Upgrade.A => new() { cost = 1 },
			_ => new() { cost = 2 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			ModEntry.Instance.KokoroApi.ConditionalActions.Make(
				ModEntry.Instance.KokoroApi.ConditionalActions.Equation(
					ModEntry.Instance.KokoroApi.Actions.MakeTimesPlayedCondition(ModEntry.Instance.KokoroApi.Actions.GetTimesPlayed(this) + 1),
					IKokoroApi.IConditionalActionApi.EquationOperator.Equal,
					ModEntry.Instance.KokoroApi.ConditionalActions.Constant(1),
					IKokoroApi.IConditionalActionApi.EquationStyle.Formal,
					hideOperator: true
				),
				new AStatus
				{
					targetPlayer = false,
					status = Reprogram.ReprogrammedStatus.Status,
					statusAmount = 1,
				}
			),
			new AStatus
			{
				targetPlayer = false,
				status = Reprogram.DeprogrammedStatus.Status,
				statusAmount = 1,
			}
		];
}
