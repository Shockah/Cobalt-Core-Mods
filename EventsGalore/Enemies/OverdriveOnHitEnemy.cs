using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.EventsGalore;

internal sealed class OverdriveOnHitEnemy : AI, IRegisterable
{
	[JsonProperty]
	private int AiCounter;

	public override string Key()
		=> $"{GetType().Namespace!}::OverdriveOnHit";

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var enemy = new OverdriveOnHitEnemy();
		DB.enemies[enemy.Key()] = enemy.GetType();
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
					damageModifier = PDamMod.armor,
					skin = "missiles_stinger",
				},
				new()
				{
					key = "bay.right",
					type = PType.missiles,
					damageModifier = PDamMod.armor,
					skin = "missiles_stinger",
					flip = true
				}
			]
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		return MoveSet(
			AiCounter++,
			() => new()
			{
				actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, 2),
				intents = [
					new IntentStatus
					{
						key = "generator",
						targetSelf = true,
						status = Status.overdrive,
						amount = 3
					},
					new IntentStatus
					{
						key = AiCounter / 2 % 2 == 0 ? "bay.right" : "bay.left",
						targetSelf = true,
						status = Status.shield,
						amount = 1
					},
				]
			},
			() => new()
			{
				actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, 2),
				intents = [
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
						key = AiCounter / 2 % 2 == 0 ? "bay.left" : "bay.right",
						targetSelf = true,
						status = Status.shield,
						amount = 1
					},
					new IntentSpawn
					{
						key = AiCounter / 2 % 2 == 0 ? "bay.right" : "bay.left",
						thing = new Missile
						{
							missileType = MissileType.normal,
							targetPlayer = true
						}
					}
				]
			}
		);
	}

	public override void OnHitByAttack(State s, Combat c, int worldX, AAttack attack)
	{
		base.OnHitByAttack(s, c, worldX, attack);
		if (c.otherShip.Get(Status.overdrive) <= 0)
			return;
		if (c.otherShip.GetPartAtWorldX(worldX) is not { } part || part.key != "generator")
			return;

		c.QueueImmediate([
			new AStatus
			{
				targetPlayer = false,
				status = Status.overdrive,
				statusAmount = -1
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.overdrive,
				statusAmount = 1
			}
		]);
	}
}
