using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.EventsGalore;

internal sealed class VolatileOverdriveEnemy : AI, IRegisterable
{
	private static readonly string MyKey = $"{typeof(VolatileOverdriveEnemy).Namespace!}::VolatileOverdrive";

	[JsonProperty]
	private int AiCounter;

	public override string Key()
		=> MyKey;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var enemy = new VolatileOverdriveEnemy();
		DB.enemies[enemy.Key()] = enemy.GetType();

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(MapFirst), nameof(MapFirst.GetEnemyPools)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Map_GetEnemyPools_Postfix)))
		);
	}

	public static void OnLoadStringsForLocale(IPluginPackage<IModManifest> package, IModHelper helper, LoadStringsForLocaleEventArgs e)
	{
		e.Localizations[$"enemy.{MyKey}.name"] = ModEntry.Instance.Localizations.Localize(["enemy", "VolatileOverdrive", "name"]);
	}

	private static void Map_GetEnemyPools_Postfix(State s, ref MapBase.MapEnemyPool __result)
	{
		__result.normal.Add(new VolatileOverdriveEnemy());
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "wasp"
		};

		var health = s.GetHarderEnemies() ? 14 : 11;
		return new Ship
		{
			x = 6,
			hull = health,
			hullMax = health,
			shieldMaxBase = 4,
			ai = this,
			chassisUnder = "chassis_pupa",
			parts = [
				new()
				{
					key = "cannon.left",
					type = PType.cannon,
					damageModifier = PDamMod.armor,
					skin = "cannon_stinger",
				},
				new()
				{
					key = "generator",
					type = PType.cockpit,
					skin = "cockpit_cicada",
				},
				new()
				{
					key = "cannon.right",
					type = PType.cannon,
					damageModifier = PDamMod.armor,
					skin = "cannon_stinger",
					flip = true,
				},
				new()
				{
					type = PType.empty,
					skin = "scaffolding",
				},
				new()
				{
					key = "bay.left",
					type = PType.missiles,
					skin = "missiles_stinger",
				},
				new()
				{
					key = "bay.right",
					type = PType.missiles,
					skin = "missiles_stinger",
					flip = true,
				}
			]
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		List<Intent> intents = [
			new IntentSpawn
			{
				key = AiCounter % 2 == 0 ? "bay.right" : "bay.left",
				thing = new Missile
				{
					missileType = MissileType.normal,
					targetPlayer = true
				}
			}
		];

		if (AiCounter % 2 == 0)
		{
			intents.Add(new IntentStatus
			{
				key = "generator",
				targetSelf = true,
				status = VolatileOverdriveStatus.Instance.Entry.Status,
				amount = 3
			});
		}
		else
		{
			intents.AddRange([
				new IntentAttack
				{
					key = "cannon.left",
					damage = 1
				},
				new IntentAttack
				{
					key = "cannon.right",
					damage = 1
				},
				new IntentStatus
				{
					key = "generator",
					targetSelf = true,
					status = Status.shield,
					amount = 2
				}
			]);
		}

		AiCounter++;
		return new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, 2),
			intents = intents
		};
	}
}
