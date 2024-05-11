using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class ActionReactionEnemy : AI, IRegisterable
{
	[JsonProperty]
	private int AiCounter;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Enemies.RegisterEnemy(new()
		{
			EnemyType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			ShouldAppearOnMap = (_, map) => map is MapThree ? BattleType.Normal : null,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["enemy", "ActionReaction", "name"]).Localize
		});
	}

	public override Ship BuildShipForSelf(State s)
	{
		character = new Character
		{
			type = "wasp"
		};

		var health = s.GetHarderEnemies() ? 20 : 14;
		return new Ship
		{
			x = 6,
			hull = health,
			hullMax = health,
			shieldMaxBase = 8,
			ai = this,
			chassisUnder = "chassis_ancient",
			parts = [
				new()
				{
					type = PType.wing,
					skin = "wing_ancient",
				},
				new()
				{
					key = "bay.left",
					type = PType.missiles,
					skin = "missiles_ancient",
				},
				new()
				{
					type = PType.empty,
					skin = "scaffolding_ancient",
				},
				new()
				{
					key = "cockpit",
					type = PType.cockpit,
					skin = "cockpit_ancient",
				},
				new()
				{
					type = PType.empty,
					skin = "scaffolding_ancient",
				},
				new()
				{
					key = "bay.right",
					type = PType.missiles,
					skin = "missiles_ancient",
				},
				new()
				{
					type = PType.wing,
					skin = "wing_ancient",
					flip = true,
				},
			]
		};
	}

	public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
	{
		List<Intent> intents = [
			new IntentSpawn
			{
				key = AiCounter % 2 == 0 ? "bay.left" : "bay.right",
				thing = AiCounter % 4 is 0 or 3
					? new ShieldDrone
					{
						targetPlayer = false
					}
					: new AttackDrone
					{
						targetPlayer = true,
						upgraded = true
					}
			}
		];

		if (AiCounter % 2 == 0)
			intents.Add(new IntentStatus
			{
				key = "cockpit",
				targetSelf = false,
				status = ActionReactionStatus.Instance.Entry.Status,
				amount = 1
			});

		AiCounter++;
		return new EnemyDecision
		{
			actions = AIHelpers.MoveToAimAt(s, ownShip, s.ship, 2),
			intents = intents
		};
	}
}
