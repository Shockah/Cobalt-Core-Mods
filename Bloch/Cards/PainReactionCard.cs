using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class PainReactionCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_BigShield,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/PainReaction.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PainReaction", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			retain = upgrade != Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ADrawCard
				{
					count = 1
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 3
					}
				}
			],
			Upgrade.B => [
				new ADrawCard
				{
					count = 1
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 2
					}
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AHeal
					{
						targetPlayer = true,
						healAmount = 2,
						canRunAfterKill = true,
					}
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new ExhaustCardAction
					{
						CardId = uuid
					}
				}
			],
			_ => [
				new ADrawCard
				{
					count = 1
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 2
					}
				}
			]
		};

	private sealed class ExhaustCardAction : CardAction
	{
		public required int CardId;

		public override Icon? GetIcon(State s)
			=> new(StableSpr.icons_exhaust, null, Colors.textMain);

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("cardtrait.exhaust")];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.FindCard(CardId) is not { } card)
				return;
			
			s.RemoveCardFromWhereverItIs(CardId);
			c.SendCardToExhaust(s, card);
			Audio.Play(Event.CardHandling);
		}
	}
}
