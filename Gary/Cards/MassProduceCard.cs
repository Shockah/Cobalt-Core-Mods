using System.Collections.Generic;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class MassProduceCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/MassProduce.png"), StableSpr.cards_goat).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MassProduce", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 0, infinite = true },
			_ => new() { cost = 0, infinite = true, floppable = true },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait },
			Upgrade.A => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait, ModEntry.Instance.KokoroApi.Heavy.Trait },
			_ => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Limited.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
				new ASpawn { thing = new ShieldDrone { targetPlayer = true } },
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.Equation(
						ModEntry.Instance.KokoroApi.Conditional.Status(CramManager.CramStatus.Status),
						IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal,
						ModEntry.Instance.KokoroApi.Conditional.Constant(0),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.Possession
					).SetShowOperator(false),
					new AFixedExhaustOtherCard { uuid = uuid }
				).AsCardAction,
			],
			_ => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false } }.Disabled(flipped),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.Equation(
						ModEntry.Instance.KokoroApi.Conditional.Status(CramManager.CramStatus.Status),
						IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal,
						ModEntry.Instance.KokoroApi.Conditional.Constant(0),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.Possession
					).SetShowOperator(false),
					new AFixedExhaustOtherCard { uuid = uuid }
				).AsCardAction.Disabled(flipped),
				new ADummyAction(),
				new ASpawn { thing = new ShieldDrone { targetPlayer = true } }.Disabled(!flipped),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.Equation(
						ModEntry.Instance.KokoroApi.Conditional.Status(CramManager.CramStatus.Status),
						IKokoroApi.IV2.IConditionalApi.EquationOperator.Equal,
						ModEntry.Instance.KokoroApi.Conditional.Constant(0),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.Possession
					).SetShowOperator(false),
					new AFixedExhaustOtherCard { uuid = uuid }
				).AsCardAction.Disabled(!flipped),
			]
		};

	private sealed class AFixedExhaustOtherCard : AExhaustOtherCard
	{
		public override Icon? GetIcon(State s)
			=> new(StableSpr.icons_exhaust, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("cardtrait.exhaust")];

		public override void Begin(G g, State s, Combat c)
		{
			timer = 0.0;
			if (s.FindCard(uuid) is not { } card)
				return;

			card.ExhaustFX();
			Audio.Play(Event.CardHandling);
			s.RemoveCardFromWhereverItIs(uuid);
			c.SendCardToExhaust(s, card);
			timer = 0.3;
		}
	}
}