using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class VolatileOverdriveEnemy : AI, IRegisterable
{
	[JsonProperty]
	private int AiCounter;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Enemies.RegisterEnemy(new()
		{
			EnemyType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			ShouldAppearOnMap = (_, map) => map is MapFirst ? BattleType.Normal : null,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "VolatileOverdrive", "name"]).Localize
		});
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
