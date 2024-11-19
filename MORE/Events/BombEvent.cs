using HarmonyLib;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class BombEvent : IRegisterable
{
	private static string EventName = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BombEnemy.RegisterEnemy(package, helper);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.PlayerWon)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_PlayerWon_Postfix))
		);

		EventName = $"{package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}";

		DB.story.all[EventName] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = true,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "chunk",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "Bomb", "1-Chunk"])
				},
				new CustomSay
				{
					who = "comp",
					loopTag = "squint",
					Text = ModEntry.Instance.Localizations.Localize(["event", "Bomb", "2-CAT"])
				},
				new CustomSay
				{
					who = "comp",
					loopTag = "intense",
					Text = ModEntry.Instance.Localizations.Localize(["event", "Bomb", "3-CAT"])
				},
			],
			choiceFunc = EventName
		};

		DB.eventChoiceFns[EventName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
	}

	[UsedImplicitly]
	private static List<Choice> GetChoices(State state)
		=> [
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "Bomb", "Choice-EnterCombat"]),
				actions = [
					new AStartCombat { ai = new AsteroidBoss() }
				]
			}
		];

	private static void Combat_PlayerWon_Postfix(/* Combat __instance, G g */)
	{
		// if (__instance.otherShip.ai is not BombEnemy)
		// 	return;
	}

	public abstract class BombEnemy : AI
	{
		public sealed class BombZone1Enemy() : BombEnemy(ZoneType.First);
		public sealed class BombZone2Enemy() : BombEnemy(ZoneType.Second);
		public sealed class BombZone3Enemy() : BombEnemy(ZoneType.Third);

		public enum ZoneType
		{
			First,
			Second,
			Third
		}

		private static ISpriteEntry SwitchBrittlePartIntentSprite = null!;
		private static ISpriteEntry SelfDestructIntentSprite = null!;

		[JsonProperty]
		private readonly ZoneType Zone;

		[JsonProperty]
		internal bool Exploded;

		[JsonConstructor]
		protected BombEnemy(ZoneType zone)
		{
			this.Zone = zone;
		}

		//new ADelayToRewards
		//			{
		//				Actions = [
		//					new ACardOffering().SetMinRarity(Rarity.uncommon)
		//				]
		//			}

		public static void RegisterEnemy(IPluginPackage<IModManifest> package, IModHelper helper)
		{
			SwitchBrittlePartIntentSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Intent/SwitchBrittlePart.png"));
			SelfDestructIntentSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Intent/SelfDestruct.png"));

			helper.Content.Enemies.RegisterEnemy(new()
			{
				EnemyType = typeof(BombZone1Enemy),
				ShouldAppearOnMap = (_, _) => null,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "Bomb", "enemy", "Bomb", "0", "name"]).Localize
			});
			helper.Content.Enemies.RegisterEnemy(new()
			{
				EnemyType = typeof(BombZone2Enemy),
				ShouldAppearOnMap = (_, _) => null,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "Bomb", "enemy", "Bomb", "1", "name"]).Localize
			});
			helper.Content.Enemies.RegisterEnemy(new()
			{
				EnemyType = typeof(BombZone3Enemy),
				ShouldAppearOnMap = (_, _) => null,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "Bomb", "enemy", "Bomb", "2", "name"]).Localize
			});
		}

		public override Ship BuildShipForSelf(State s)
		{
			character = new Character
			{
				type = "chunk"
			};

			var baseHealth = Zone switch
			{
				ZoneType.First => 16,
				ZoneType.Second => 24,
				ZoneType.Third => 32,
				_ => throw new ArgumentException()
			};

			var health = s.GetHarderEnemies() ? (int)(baseHealth * 1.25) : baseHealth;
			var ship = new Ship
			{
				x = 8,
				hull = health,
				hullMax = health,
				shieldMaxBase = 0,
				ai = this,
				chassisUnder = "chassis_goliath",
				parts = Zone switch
				{
					ZoneType.First => [
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere",
							flip = true,
						},
						new()
						{
							type = PType.cockpit,
							skin = "cockpit_cicada"
						},
						new()
						{
							type = PType.cockpit,
							damageModifier = PDamMod.brittle,
							skin = "cockpit_cicada2"
						},
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere"
						},
					],
					ZoneType.Second => [
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere",
							flip = true,
						},
						new()
						{
							type = PType.cockpit,
							skin = "cockpit_cicada"
						},
						new()
						{
							type = PType.cockpit,
							damageModifier = PDamMod.brittle,
							skin = "cockpit_cicada2"
						},
						new()
						{
							type = PType.cockpit,
							skin = "cockpit_cicada3"
						},
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere"
						},
					],
					ZoneType.Third => [
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere",
							flip = true,
						},
						new()
						{
							type = PType.cockpit,
							damageModifier = PDamMod.brittle,
							skin = "cockpit_cicada2"
						},
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere"
						},
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere",
							flip = true,
						},
						new()
						{
							type = PType.cockpit,
							skin = "cockpit_cicada3"
						},
						new()
						{
							type = PType.wing,
							skin = "cockpit_sphere"
						},
					],
					_ => throw new ArgumentException()
				}
			};

			if (ship.parts.FirstOrDefault(p => p.damageModifier == PDamMod.brittle) is { } brittlePart)
				ModEntry.Instance.Helper.ModData.SetModData(brittlePart, "IsSwitchableBrittle", true);

			ship.Set(SelfDestructTimerStatus.Instance.Entry.Status, 6);

			return ship;
		}

		public override EnemyDecision PickNextIntent(State s, Combat c, Ship ownShip)
		{
			List<Intent> intents = [];

			List<int> availablePartIndexes = Enumerable.Range(0, ownShip.parts.Count)
				.Where(i => ownShip.parts[i].damageModifier != PDamMod.brittle && !ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(ownShip.parts[i], "IsSwitchableBrittle"))
				.ToList();
			if (availablePartIndexes.Count != 0)
				intents.Add(new SwitchBrittlePartIntent
				{
					fromX = availablePartIndexes[s.rngAi.NextInt() % availablePartIndexes.Count]
				});

			if (ownShip.Get(SelfDestructTimerStatus.Instance.Entry.Status) <= 0)
			{
				availablePartIndexes = Enumerable.Range(0, ownShip.parts.Count)
					.Where(i => intents.All(intent => intent.fromX != i))
					.ToList();
				if (availablePartIndexes.Count != 0)
					intents.Add(new SelfDestructIntent
					{
						fromX = availablePartIndexes[s.rngAi.NextInt() % availablePartIndexes.Count],
						PercentMaxDamage = 0.5
					});
			}

			return new EnemyDecision
			{
				actions = [],
				intents = intents
			};
		}

		internal sealed class SelfDestructTimerStatus : IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook
		{
			internal static SelfDestructTimerStatus Instance { get; private set; } = null!;
			internal IStatusEntry Entry { get; private set; } = null!;

			static void IRegisterable.Register(IPluginPackage<IModManifest> package, IModHelper helper)
			{
				Instance = new();

				Instance.Entry = helper.Content.Statuses.RegisterStatus("SelfDestructTimer", new()
				{
					Definition = new()
					{
						icon = StableSpr.icons_timeStop,
						color = DB.statuses[Status.timeStop].color,
						affectedByTimestop = true,
						isGood = false,
					},
					Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "SelfDestructTimer", "name"]).Localize,
					Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "SelfDestructTimer", "description"]).Localize
				});

				ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(Instance);
			}

			public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
			{
				if (args.Status != Entry.Status)
					return false;
				if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
					return false;

				args.Amount -= Math.Sign(args.Amount);
				return false;
			}
		}

		private sealed class SwitchBrittlePartIntent : Intent
		{
			public override Spr? GetSprite(State s)
				=> SwitchBrittlePartIntentSprite.Sprite;

			public override List<Tooltip>? GetTooltips(State s, Combat c, Ship fromShip)
				=> [
					new TTText(ModEntry.Instance.Localizations.Localize(["intent", "SwitchBrittlePart"])),
					new TTGlossary("parttrait.brittle")
				];

			public override void Apply(State s, Combat c, Ship fromShip, int actualX)
			{
				base.Apply(s, c, fromShip, actualX);

				foreach (var part in fromShip.parts)
				{
					if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(part, "IsSwitchableBrittle"))
						continue;

					ModEntry.Instance.Helper.ModData.RemoveModData(part, "IsSwitchableBrittle");
					if (part.damageModifier == PDamMod.brittle)
						part.damageModifier = PDamMod.none;
				}

				if (fromShip.GetPartAtLocalX(actualX) is not { } newPart)
					return;
				if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(newPart, "IsSwitchableBrittle"))
					return;

				newPart.damageModifier = PDamMod.brittle;
				ModEntry.Instance.Helper.ModData.SetModData(newPart, "IsSwitchableBrittle", true);
			}
		}

		internal sealed class SelfDestructIntent : Intent
		{
			public int FlatDamage = 0;
			public double PercentCurrentDamage = 0;
			public double PercentMaxDamage;
			public bool PreventDeath = false;
			public bool HurtShieldsFirst = true;

			public override Spr? GetSprite(State s)
				=> SelfDestructIntentSprite.Sprite;

			public override List<Tooltip>? GetTooltips(State s, Combat c, Ship fromShip)
				=> [
					new TTText(ModEntry.Instance.Localizations.Localize(["intent", "SelfDestruct"], new { Damage = GetTotalDamage(s) }))
				];

			public override void Apply(State s, Combat c, Ship fromShip, int actualX)
			{
				base.Apply(s, c, fromShip, actualX);
				if (c.otherShip.ai is BombEnemy enemy)
					enemy.Exploded = true;

				c.QueueImmediate([
					new AHurt
					{
						targetPlayer = false,
						hurtAmount = 1000
					},
					new AHurt
					{
						targetPlayer = true,
						hurtAmount = GetTotalDamage(s),
						hurtShieldsFirst = HurtShieldsFirst,
						canRunAfterKill = true
					}
				]);
			}

			private int GetTotalDamage(State state)
			{
				var damageToDeal = FlatDamage + (int)(state.ship.hull * PercentCurrentDamage) + (int)(state.ship.hullMax * PercentMaxDamage);
				if (PreventDeath)
					damageToDeal = Math.Min(damageToDeal, state.ship.hull - 1);
				return damageToDeal;
			}
		}
	}
}
