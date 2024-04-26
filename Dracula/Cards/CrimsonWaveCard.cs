using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class CrimsonWaveCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("CrimsonWave", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.rare,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_eunice,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CrimsonWave", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 4 : 3,
			retain = upgrade == Upgrade.A,
			exhaust = upgrade != Upgrade.B,
			singleUse = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "CrimsonWave", "description", upgrade.ToString()], new { Damage = GetDmg(state, 1) })
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		for (var i = 0; i < s.ship.parts.Count; i++)
		{
			if (s.ship.parts[i].type == PType.empty)
				continue;

			actions.Add(new AAttack
			{
				fromX = i,
				multiCannonVolley = true,
				fast = true,
				damage = GetDmg(s, 1),
				stunEnemy = true
			}.SetLifestealMultiplier(upgrade == Upgrade.B ? 1 : 0));
		}
		actions.Add(new LifestealManager.AApplyLifesteal { TargetPlayer = true });
		actions.Add(new HilightAction());
		return actions;
	}

	private sealed class HilightAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
		{
			if (s.route is not Combat combat)
				return base.GetTooltips(s);

			for (var i = 0; i < s.ship.parts.Count; i++)
			{
				if (s.ship.parts[i].type == PType.empty)
					continue;

				s.ship.parts[i].hilight = true;
				if (combat.stuff.TryGetValue(s.ship.x + i, out var @object))
					@object.hilight = 2;
			}

			return base.GetTooltips(s);
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
		}
	}
}
