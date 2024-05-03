using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class DistressCard : Card, IRegisterable
{
	private static ISpriteEntry DiscardOtherIcon = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		DiscardOtherIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Actions/DiscardOther.png"));

		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Distress.png"), StableSpr.cards_Shield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Distress", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			infinite = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 2
				},
				new ADiscard
				{
					count = 3
				}
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: ModEntry.Instance.KokoroApi.ConditionalActions.Equation(
						lhs: ModEntry.Instance.KokoroApi.ConditionalActions.HandConstant(c.hand.Count),
						@operator: IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual,
						rhs: ModEntry.Instance.KokoroApi.ConditionalActions.Constant(3),
						style: IKokoroApi.IConditionalActionApi.EquationStyle.Possession,
						hideOperator: true
					),
					action: ModEntry.Instance.KokoroApi.Actions.MakeContinue(out var continueId)
				),
				..ModEntry.Instance.KokoroApi.Actions.MakeContinued(continueId, [
					new AStatus
					{
						targetPlayer = true,
						status = AuraManager.VeilingStatus.Status,
						statusAmount = 1
					},
					new DiscardOtherAction
					{
						Count = 2,
						IgnoreId = uuid
					}
				])
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 1
				},
				new ADiscard
				{
					count = 2
				}
			]
		};

	private sealed class DiscardOtherAction : CardAction
	{
		public required int Count;
		public required int IgnoreId;

		public override Icon? GetIcon(State s)
			=> new(DiscardOtherIcon.Sprite, Count, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{GetType().Namespace!}::DiscardOther")
				{
					Icon = DiscardOtherIcon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "DiscardOther", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "DiscardOther", "description"], new { Count }),
				}
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var cardsToDiscard = c.hand
				.Where(card => card.uuid != IgnoreId)
				.Shuffle(s.rngShuffle)
				.Take(Count)
				.ToList();

			for (var i = 0; i < cardsToDiscard.Count; i++)
			{
				var card = cardsToDiscard[i];
				c.hand.Remove(card);
				card.waitBeforeMoving = i * 0.05;
				card.OnDiscard(s, c);
				c.SendCardToDiscard(s, card);
			}

			if (cardsToDiscard.Count != 0)
				Audio.Play(Event.CardHandling);
		}
	}
}
