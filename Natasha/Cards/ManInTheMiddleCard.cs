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
	internal record SerializableObjectEntry(
		string UniqueName,
		double Weight
	);

	internal static readonly Dictionary<string, INatashaApi.ManInTheMiddleStaticObjectEntry> RegisteredObjects = new List<INatashaApi.ManInTheMiddleStaticObjectEntry>()
	{
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
	}.ToDictionary(e => e.UniqueName, e => e);

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
		public bool Random;

		public override List<Tooltip> GetTooltips(State s)
		{
			if (s.route is Combat combat)
			{
				if (GetIdealWorldX(s, combat) is { } idealWorldX && s.ship.GetPartAtWorldX(idealWorldX) is { } part)
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
		{
			var entries = state.ship.parts
				.Select((p, i) => (WorldX: state.ship.x + i, Part: p))
				.Where(e => e.Part.type != PType.empty)
				.Select(e => (WorldX: e.WorldX, PlayerPart: e.Part, EnemyPart: combat.otherShip.GetPartAtWorldX(e.WorldX)))
				.Select(e => (
					WorldX: e.WorldX,
					PlayerPart: e.PlayerPart,
					EnemyPart: e.EnemyPart,
					Midrow: combat.stuff.GetValueOrDefault(e.WorldX),
					Attack: e.EnemyPart?.intent as IntentAttack,
					Missile: e.EnemyPart?.intent switch
					{
						IntentMissile missileIntent => missileIntent.missileType,
						IntentSpawn spawnIntent => spawnIntent.thing is Missile missile ? missile.missileType : null,
						_ => (MissileType?)null
					},
					Spawn: e.EnemyPart?.intent is IntentSpawn { thing: not Missile } spawnIntent2 ? spawnIntent2.thing : null
				))
				.OrderBy(e => Math.Abs(e.WorldX - state.ship.x - state.ship.parts.Count / 2.0))
				.ToList();

			var attackEntry = entries
				.Where(e => e.Midrow is null && e.Attack is not null)
				.Select(e => (WorldX: e.WorldX, PlayerPart: e.PlayerPart, Hits: e.Attack!.multiHit, Damage: GetModifiedDamage(e.Attack!.damage, e.PlayerPart), ExtraThreat: (e.Attack!.status is null ? 0 : 0.45) + (e.Attack!.cardOnHit is null ? 0 : 0.45)))
				.Where(e => e.Damage > 0 || e.ExtraThreat > 0)
				.OrderByDescending(e => e.Damage + e.ExtraThreat)
				.ThenBy(e => e.Hits)
				.FirstOrNull();

			if (attackEntry is not null)
				return attackEntry.Value.WorldX;

			var missileEntry = entries
				.Where(e => e.Midrow is Missile)
				.Select(e => (WorldX: e.WorldX, PlayerPart: e.PlayerPart, Damage: GetModifiedDamage(GetMissileDamage(((Missile)e.Midrow!).missileType), e.PlayerPart), ExtraThreat: GetMissileExtraThreat(((Missile)e.Midrow!).missileType)))
				.Where(e => e.Damage > 0 || e.ExtraThreat > 0)
				.OrderByDescending(e => e.Damage + e.ExtraThreat)
				.FirstOrNull();

			if (missileEntry is not null)
				return missileEntry.Value.WorldX;

			missileEntry = entries
				.Where(e => e.Midrow is null && e.Missile is not null)
				.Select(e => (WorldX: e.WorldX, PlayerPart: e.PlayerPart, Damage: GetModifiedDamage(GetMissileDamage(e.Missile!.Value), e.PlayerPart), ExtraThreat: GetMissileExtraThreat(e.Missile!.Value)))
				.Where(e => e.Damage > 0 || e.ExtraThreat > 0)
				.OrderByDescending(e => e.Damage + e.ExtraThreat)
				.FirstOrNull();

			if (missileEntry is not null)
				return missileEntry.Value.WorldX;

			var entry = entries
				.Where(e => e.Midrow is not null && (e.Midrow.IsHostile() || !e.Midrow.IsFriendly()) && (e.Midrow.GetActions(state, combat)?.Count ?? 0) != 0)
				.FirstOrNull();

			if (entry is not null)
				return entry.Value.WorldX;

			entry = entries
				.Where(e => e.Midrow is null && e.Spawn is not null && (e.Spawn.IsHostile() || !e.Spawn.IsFriendly()) && (e.Spawn.GetActions(state, combat)?.Count ?? 0) != 0)
				.FirstOrNull();

			if (entry is not null)
				return entry.Value.WorldX;

			return entries
				.OrderByDescending(e => e.Midrow is null)
				.ThenByDescending(e => e.EnemyPart is not null && e.EnemyPart.type != PType.empty)
				.FirstOrNull()?.WorldX;

			static int GetMissileDamage(MissileType type)
				=> type switch
				{
					MissileType.corrode => 5,
					MissileType.heavy or MissileType.breacher => 3,
					MissileType.shaker => 1,
					_ => 2
				};

			static double GetMissileExtraThreat(MissileType type)
				=> type == MissileType.shaker ? 0.45 : 0;

			static int GetModifiedDamage(int damage, Part part)
				=> Math.Max(part.damageModifier switch
				{
					PDamMod.armor => damage - 1,
					PDamMod.weak => damage + 1,
					PDamMod.brittle => damage * 2,
					_ => damage
				}, 0);
		}

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
				if (!RegisteredObjects.TryGetValue(chosenSerializableEntry.UniqueName, out var chosenEntry))
					continue;

				queue.Remove(chosenSerializableEntry);
				queue.Add(chosenSerializableEntry with { Weight = chosenEntry.WeightProvider?.Invoke(state, chosenSerializableEntry.Weight) ?? chosenSerializableEntry.Weight });
				return chosenEntry.Factory(state);
			}
		}

		private static List<SerializableObjectEntry> ObtainObjectQueue(Combat combat)
		{
			var queue = ModEntry.Instance.Helper.ModData.ObtainModData<List<SerializableObjectEntry>>(combat, $"{nameof(ManInTheMiddleCard)}::ObjectQueue");

			for (var i = queue.Count - 1; i >= 0; i--)
				if (!RegisteredObjects.ContainsKey(queue[i].UniqueName))
					queue.RemoveAt(i);

			if (queue.Count == 0)
				queue.AddRange(RegisteredObjects.Values.Select(e => new SerializableObjectEntry(e.UniqueName, e.InitialWeight)));

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
