using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class ManInTheMiddleCard : Card, IRegisterable, IHasCustomCardTraits
{
	internal record ObjectEntry(
		string UniqueName,
		Func<State, StuffBase> Factory,
		double InitialWeight = 1,
		Func<State, double, double>? WeightProvider = null
	);

	internal record SerializableObjectEntry(
		string UniqueName,
		double Weight
	);

	internal static readonly List<ObjectEntry> RegisteredObjects = [
		new($"{typeof(ModEntry).Namespace!}::Asteroid", _ => new Asteroid()),
		new($"{typeof(ModEntry).Namespace!}::Geode", _ => new Geode(), 0.5),
		new($"{typeof(ModEntry).Namespace!}::Mine", _ => new SpaceMine()),
		new($"{typeof(ModEntry).Namespace!}::BigMine", _ => new SpaceMine { bigMine = true }, 0.5),
		new($"{typeof(ModEntry).Namespace!}::AttackDrone", _ => new AttackDrone()),
		new($"{typeof(ModEntry).Namespace!}::AttackDroneMk2", _ => new AttackDrone { upgraded = true }, 0.5),
		new($"{typeof(ModEntry).Namespace!}::ShieldDrone", _ => new ShieldDrone { targetPlayer = true }),
		new($"{typeof(ModEntry).Namespace!}::EnergyDrone", _ => new EnergyDrone { targetPlayer = true }, 0.25),
		new($"{typeof(ModEntry).Namespace!}::JupiterDrone", _ => new JupiterDrone(), 0.25),
		new($"{typeof(ModEntry).Namespace!}::SportOrb", _ => new Football(), 0.1, (_, w) => w / 2.0)
	];

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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ManInTheMiddle.png"), StableSpr.cards_goat).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ManInTheMiddle", "name"]).Localize
		});

		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.None, 3);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.A, 5);
		Limited.SetBaseLimitedUses(entry.UniqueName, Upgrade.B, 3);

		ModEntry.Instance.KokoroApi.RegisterCardRenderHook(new Hook(), 0);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { Limited.Trait };

	public override CardData GetData(State state)
		=> new() { cost = 1, description = ModEntry.Instance.Localizations.Localize(["card", "ManInTheMiddle", "description", upgrade.ToString()]) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [new Action { Random = true }],
			_ => [new Action { Random = false }]
		};

	private sealed class Action : CardAction
	{
		public bool Random = false;

		public override List<Tooltip> GetTooltips(State s)
		{
			if (s.route is Combat combat)
			{
				if (GetIdealWorldX(s, s.route as Combat ?? DB.fakeCombat) is { } idealWorldX && s.ship.GetPartAtWorldX(idealWorldX) is { } part)
				{
					part.hilight = true;
					if (combat.stuff.TryGetValue(idealWorldX, out var @object))
						@object.hilight = 2;
				}
				else
				{
					_ = new ASpawn { thing = new Asteroid() }.GetTooltips(s);
				}
			}

			return [
				new TTGlossary("action.spawn"),
				.. new Asteroid().GetTooltips()
			];
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var @object = Random ? CreateNextObject(s, c) : new Asteroid();
			c.QueueImmediate(
				GetIdealWorldX(s, c) is { } idealWorldX
					? new ASpawn { fromX = idealWorldX - s.ship.x, thing = @object }
					: new ASpawn { thing = @object }
			);
		}

		private static int? GetIdealWorldX(State state, Combat combat)
			=> state.ship.parts
				.Select((p, i) => (WorldX: state.ship.x + i, Part: p))
				.Where(e => e.Part.type != PType.empty)
				.Select(e => (WorldX: e.WorldX, PlayerPart: e.Part, EnemyPart: combat.otherShip.GetPartAtWorldX(e.WorldX)))
				.Where(e => e.EnemyPart is not null)
				.Select(e => (WorldX: e.WorldX, PlayerPart: e.PlayerPart, EnemyPart: e.EnemyPart!, Midrow: combat.stuff.GetValueOrDefault(e.WorldX)))
				.OrderByDescending(e => e.EnemyPart.intent switch
				{
					IntentAttack attackIntent => e.Midrow is null ? attackIntent.damage : 0,
					IntentMissile missileIntent => e.Midrow is null ? missileIntent.missileType switch
					{
						MissileType.shaker => 1,
						MissileType.normal or MissileType.seeker or MissileType.punch => 2,
						MissileType.heavy or MissileType.breacher => 3,
						MissileType.corrode => 5,
						_ => 2
					} : 0,
					IntentSpawn spawnIntent => e.Midrow is null || spawnIntent.thing.bubbleShield ? 2 : 0,
					_ => 0
				})
				.ThenByDescending(e => Math.Abs((e.WorldX - state.ship.x) / 2.0 - state.ship.parts.Count / 2.0))
				.FirstOrNull()?.WorldX;

		private static StuffBase CreateNextObject(State state, Combat combat)
		{
			var queue = ObtainObjectQueue(combat);

			var weighted = new WeightedRandom<SerializableObjectEntry>();
			foreach (var serializableEntry in queue)
				weighted.Add(new(serializableEntry.Weight, serializableEntry));

			while (true)
			{
				if (weighted.Items.Count == 0)
					return new Asteroid();

				var chosenSerializableEntry = weighted.Next(state.rngActions, consume: true);
				if (RegisteredObjects.FirstOrDefault(e => e.UniqueName == chosenSerializableEntry.UniqueName) is not { } chosenEntry)
					continue;

				queue.Remove(chosenSerializableEntry);
				queue.Add(chosenSerializableEntry with { Weight = chosenEntry.WeightProvider?.Invoke(state, chosenSerializableEntry.Weight) ?? chosenSerializableEntry.Weight });
				return chosenEntry.Factory(state);
			}
		}

		private static List<SerializableObjectEntry> ObtainObjectQueue(Combat combat)
		{
			var queue = ModEntry.Instance.Helper.ModData.ObtainModData<List<SerializableObjectEntry>>(combat, $"{typeof(ManInTheMiddleCard).Name}::ObjectQueue");

			for (var i = queue.Count - 1; i >= 0; i--)
				if (!RegisteredObjects.Any(e => e.UniqueName == queue[i].UniqueName))
					queue.RemoveAt(i);

			if (queue.Count == 0)
				foreach (var entry in RegisteredObjects)
					queue.Add(new(entry.UniqueName, entry.InitialWeight));

			return queue;
		}
	}

	private sealed class Hook : ICardRenderHook
	{
		public Font? ReplaceTextCardFont(G g, Card card)
		{
			if (card is not ManInTheMiddleCard)
				return null;
			return ModEntry.Instance.KokoroApi.PinchCompactFont;
		}
	}
}
