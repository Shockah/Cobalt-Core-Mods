using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class RemoteDetonatorCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DynaDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_hacker,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/RemoteDetonator.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RemoteDetonator", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			retain = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "RemoteDetonator", "description"], new { Damage })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new DetonateAllAction
			{
				Damage = Damage
			}
		];

	private int Damage
		=> upgrade == Upgrade.A ? 2 : 1;

	private sealed class DetonateAllAction : CardAction
	{
		public required int Damage;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			TriggerOnShip(c, s.ship);
			TriggerOnShip(c, c.otherShip);
		}

		private void TriggerOnShip(Combat c, Ship ship)
		{
			c.QueueImmediate(Enumerable.Range(0, ship.parts.Count).Select(i => new DetonateAction
			{
				TargetPlayer = ship.isPlayerShip,
				Damage = Damage,
				WorldX = ship.x + i
			}));
		}
	}

	private sealed class DetonateAction : CardAction
	{
		public required bool TargetPlayer;
		public required int Damage;
		public required int WorldX;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var targetShip = TargetPlayer ? s.ship : c.otherShip;
			if (targetShip.GetPartAtWorldX(WorldX) is not { } part || part.type == PType.empty || part.GetStickedCharge() is null)
				return;

			ChargeManager.TriggerChargeIfAny(s, c, part, TargetPlayer);
			c.QueueImmediate(new AHurt
			{
				targetPlayer = TargetPlayer,
				hurtAmount = Damage
			});
		}
	}
}
