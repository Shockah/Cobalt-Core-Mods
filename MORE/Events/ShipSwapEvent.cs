﻿using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shockah.Shared;

namespace Shockah.MORE;

internal sealed class ShipSwapEvent : IRegisterable
{
	private static string EventName = null!;
	private static IArtifactEntry RippedPartArtifactEntry = null!;

	internal static readonly HashSet<string> ArtifactsToReapply = [
		nameof(ArmoredBay),
		nameof(BrokenGlasses),
		nameof(DemonThrusters),
		nameof(DirtyEngines),
		nameof(FlowState),
		nameof(GlassCannon),
		nameof(HiFreqIntercom),
		nameof(NextGenInsulation),
		nameof(PowerDiversion),
		nameof(Prototype22),
		nameof(ShardCollector),
		nameof(ShieldReserves),
		nameof(SubzeroHeatsinks),
		nameof(ThermoReactor),
		nameof(TridimensionalCockpit),
	];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		RippedPartArtifact.RegisterArtifact(helper);
		
		EventName = $"{package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}";

		DB.story.all[EventName] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = true,
			oncePerRun = true,
			zones = ["zone_lawless", "zone_three"],
			bg = "BGSunshine",
			lines = [
				new CustomSay
				{
					who = "selene",
					loopTag = "squint",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "1-Selene"])
				},
				new CustomSay
				{
					who = "selene",
					loopTag = "explains",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "2-Selene"])
				},
				new CustomSay
				{
					who = "selene",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "3-Selene"])
				},
				new CustomSay
				{
					who = "selene",
					loopTag = "sly",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "4-Selene"])
				},
			],
			choiceFunc = EventName
		};
		DB.story.all[$"{EventName}::Yes"] = new()
		{
			type = NodeType.@event,
			bg = "BGSunshine",
			lines = [
				new CustomSay
				{
					who = "selene",
					loopTag = "sly",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Yes-1-Selene"])
				},
			]
		};
		
		DB.story.all[$"{EventName}::Part"] = new()
		{
			type = NodeType.@event,
			bg = "BGSunshine",
			lines = [
				new CustomSay
				{
					who = "selene",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Part-1-Selene"])
				},
			],
			choiceFunc = $"{EventName}::Part"
		};
		DB.story.all[$"{EventName}::No"] = new()
		{
			type = NodeType.@event,
			bg = "BGSunshine",
			lines = [
				new CustomSay
				{
					who = "selene",
					loopTag = "squint",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "No-1-Selene"])
				},
			]
		};

		DB.eventChoiceFns[EventName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
		DB.eventChoiceFns[$"{EventName}::Part"] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetPartChoices));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_GetTooltips_Postfix))
		);
	}

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		var node = DB.story.all[EventName];
		node.never = settings.DisabledEvents.Contains(MoreEvent.ShipSwap) ? true : null;
		node.dontCountForProgression = settings.DisabledEvents.Contains(MoreEvent.ShipSwap);
		RippedPartArtifactEntry.Configuration.Meta.pools = RippedPartArtifactEntry.Configuration.Meta.pools
			.Where(p => p != ArtifactPool.Unreleased)
			.Concat(settings.DisabledEvents.Contains(MoreEvent.ShipSwap) ? [ArtifactPool.Unreleased] : [])
			.ToArray();
	}

	private static List<Choice> GetChoices(State state)
	{
		var eventRngData = GetEventRngData(state);

		return [
			.. Enumerable.Range(0, eventRngData.NewShipKeys.Count)
				.Select(i => new Choice
				{
					label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-Yes", i.ToString()], new { ShipName = Loc.T($"ship.{eventRngData.NewShipKeys[i]}.name") }),
					key = $"{EventName}::Yes",
					actions = [new ASwapShip { NewShipKey = eventRngData.NewShipKeys[i] }],
				}),
			.. eventRngData.PartToRipIndex is null ? Array.Empty<Choice>() : [new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-Part", "choice"]),
				key = $"{EventName}::Part",
				actions = [new ATooltipAction { Tooltips = [new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(ARipPart)}::ChoicePart")
				{
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-Part", "title"]),
					Description = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-Part", "description"]),
				}] }]
			}],
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-No"]),
				key = $"{EventName}::No",
			},
		];
	}

	private static List<Choice> GetPartChoices(State state)
	{
		var declineChoice = new Choice
		{
			label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Part-Choice-Decline"]),
			key = EventName,
			actions = [new ADelayedSkipDialogue()]
		};

		var eventRngData = GetEventRngData(state);
		if (eventRngData.PartToRipIndex is not { } partToRipIndex)
			return [declineChoice];
		
		var acceptChoice = new Choice
		{
			label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Part-Choice-Accept"]),
			key = $"{EventName}::Yes",
			actions = [
				new AShipUpgrades
				{
					actions = [
						new ARipPart { PartToRipIndex = partToRipIndex },
						new AAddArtifact { artifact = new RippedPartArtifact { RippedPartType = state.ship.parts[partToRipIndex].type } }
					],
				},
			],
		};

		return [acceptChoice, declineChoice];
	}

	private static EventRngData GetEventRngData(State state)
	{
		var rand = new Rand(state.rngCurrentEvent.seed);
		
		var currentShipKey = state.ship.key;
		var otherShips = StarterShip.ships.Keys
			.Where(key => currentShipKey != key)
			.ToList();
		var newShipKeys = otherShips.Shuffle(rand).Take(3).ToList();
		
		if (!state.ship.parts.Where(p => p.type is PType.wing or PType.comms or PType.cockpit or PType.missiles or PType.cannon).Skip(1).Any())
			return new(newShipKeys, null);

		var potentialPartTypesToRip = new WeightedRandom<PType>();
		if (state.ship.parts.Any(p => p.type == PType.wing))
			potentialPartTypesToRip.Add(new(1, PType.wing));
		if (state.ship.parts.Any(p => p.type == PType.comms))
			potentialPartTypesToRip.Add(new(1, PType.comms));
		if (state.characters.All(c => c.deckType != Deck.hacker) && state.ship.parts.Any(p => p.type == PType.cockpit))
			potentialPartTypesToRip.Add(new(0.5, PType.cockpit));
		if (potentialPartTypesToRip.Items.Count == 0 && state.ship.parts.Any(p => p.type == PType.missiles))
			potentialPartTypesToRip.Add(new(1, PType.missiles));
		if (potentialPartTypesToRip.Items.Count == 0 && state.ship.parts.Any(p => p.type == PType.cannon))
			potentialPartTypesToRip.Add(new(1, PType.cannon));

		if (potentialPartTypesToRip.Items.Count == 0)
			return new(newShipKeys, null);

		var partTypeToRip = potentialPartTypesToRip.Next(rand);
		var potentialPartsToRip = state.ship.parts
			.Select((p, i) => (Part: p, Index: i))
			.Where(e => e.Part.type == partTypeToRip)
			.ToList();

		return potentialPartsToRip.Count switch
		{
			0 => new(newShipKeys, null),
			1 => new(newShipKeys, potentialPartsToRip[0].Index),
			_ => new(newShipKeys, potentialPartsToRip[rand.NextInt() % potentialPartsToRip.Count].Index)
		};
	}

	private sealed class ASwapShip : CardAction
	{
		public required string NewShipKey;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			
			var currentShipKey = s.ship.key;
			if (!StarterShip.ships.TryGetValue(NewShipKey, out var newShipStarter))
				return;
			if (!StarterShip.ships.TryGetValue(currentShipKey, out var currentShipStarter))
				return;

			List<CardAction> moreActions = [];
			HashSet<string> starterArtifactKeysToRemove = [];
			HashSet<string> commonArtifactKeysToRemove = [];
			HashSet<string> bossArtifactKeysToRemove = [];
			List<Artifact> artifactsToAdd = [];

			foreach (var artifact in currentShipStarter.artifacts)
				if (s.EnumerateAllArtifacts().Any(a => a.Key() == artifact.Key()))
					MarkArtifactForRemoval(artifact, isStarter: true);
			foreach (var artifactType in GetExclusiveArtifactTypes(currentShipKey))
				if (s.EnumerateAllArtifacts().FirstOrDefault(a => a.GetType() == artifactType) is { } artifact)
					MarkArtifactForRemoval(artifact, isStarter: false);
			foreach (var artifact in newShipStarter.artifacts)
				MarkArtifactForAddition(artifact);

			foreach (var artifact in s.EnumerateAllArtifacts())
			{
				if (!ArtifactsToReapply.Contains(artifact.Key()))
					continue;
				moreActions.Add(new ALoseArtifact { artifactType = artifact.Key() });
				MarkArtifactForAddition(artifact);
			}
			
			moreActions.AddRange(starterArtifactKeysToRemove.Select(a => new ALoseArtifact { artifactType = a }));
			moreActions.AddRange(commonArtifactKeysToRemove.Select(a => new ALoseArtifact { artifactType = a }));
			moreActions.AddRange(bossArtifactKeysToRemove.Select(a => new ALoseArtifact { artifactType = a }));

			moreActions.Add(new AShipUpgrades { actions = [new AActuallySwapShipBody { NewShipKey = NewShipKey }] });
			moreActions.Add(new ATakeCardsUntilSkip { Cards = newShipStarter.cards.Select(card => card.CopyWithNewId()).ToList() });

			if (commonArtifactKeysToRemove.Count != 0 || bossArtifactKeysToRemove.Count != 0)
			{
				var fakeState = Mutil.DeepCopy(s);
				foreach (var action in Mutil.DeepCopy(moreActions))
					action.Begin(g, fakeState, DB.fakeCombat);

				int AddArtifacts(IEnumerable<Artifact> all, int amount)
				{
					foreach (var artifact in all)
					{
						if (amount <= 0)
							return amount;
						if (ArtifactReward.GetBlockedArtifacts(fakeState).Contains(artifact.GetType()))
							continue;

						moreActions.Add(new AAddArtifact { artifact = artifact });
						new AAddArtifact { artifact = artifact }.Begin(g, fakeState, DB.fakeCombat);
						amount--;
					}
					return amount;
				}
				
				var rand = new Rand(s.rngCurrentEvent.seed + 7498);
				var exclusiveArtifacts = GetExclusiveArtifactTypes(NewShipKey)
					.Select(t => (Artifact)Activator.CreateInstance(t)!)
					.Where(a => !a.GetMeta().pools.Contains(ArtifactPool.Unreleased))
					.Shuffle(rand)
					.ToList();

				if (commonArtifactKeysToRemove.Count != 0)
				{
					var leftToAdd = AddArtifacts(exclusiveArtifacts.Where(a => a.GetMeta().pools.Contains(ArtifactPool.Common)), commonArtifactKeysToRemove.Count);
					for (var i = 0; i < leftToAdd; i++)
						moreActions.Add(new AArtifactOffering
						{
							amount = s.GetDifficulty() >= 2 ? 2 : 3,
							limitPools = [ArtifactPool.Common]
						});
				}
				if (bossArtifactKeysToRemove.Count != 0)
				{
					var leftToAdd = AddArtifacts(exclusiveArtifacts.Where(a => a.GetMeta().pools.Contains(ArtifactPool.Boss)), bossArtifactKeysToRemove.Count);
					for (var i = 0; i < leftToAdd; i++)
						moreActions.Add(new AArtifactOffering
						{
							amount = s.GetDifficulty() >= 2 ? 2 : 3,
							limitPools = [ArtifactPool.Boss]
						});
				}
			}
			
			moreActions.AddRange(artifactsToAdd.Select(a => new AAddArtifact { artifact = Mutil.DeepCopy(a) }));
			
			s.GetCurrentQueue().InsertRange(0, moreActions);

			void MarkArtifactForAddition(Artifact artifact)
				=> artifactsToAdd.Add(artifact);

			void MarkArtifactForRemoval(Artifact artifact, bool isStarter)
			{
				var key = artifact.Key();
				var meta = artifact.GetMeta();
				if (meta.pools.Contains(ArtifactPool.Boss))
					MarkArtifactKeyForRemoval(key, true, isStarter);
				else if (meta.pools.Contains(ArtifactPool.Common))
					MarkArtifactKeyForRemoval(key, false, isStarter);
				else if (isStarter)
					MarkArtifactKeyForRemoval(key, false, isStarter);
			}

			void MarkArtifactKeyForRemoval(string key, bool isBoss, bool isStarter)
			{
				if (isStarter)
					starterArtifactKeysToRemove.Add(key);
				else if (isBoss)
					bossArtifactKeysToRemove.Add(key);
				else
					commonArtifactKeysToRemove.Add(key);
			}
		}

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new TTText(ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Action-Tooltip"])),
				new TTText($"<c=textChoice>{Loc.T($"ship.{NewShipKey}.name")}</c>\n{Loc.T($"ship.{NewShipKey}.desc")}")
			];

		private static IEnumerable<Type> GetExclusiveArtifactTypes(string shipKey)
		{
			switch (shipKey)
			{
				case "artemis":
					yield return typeof(HunterWings);
					break;
				case "ares":
					yield return typeof(AresCannonV2);
					yield return typeof(ControlRodsV2);
					break;
				case "jupiter":
					yield return typeof(JupiterDroneHubV2);
					yield return typeof(JupiterDroneHubV3);
					yield return typeof(JupiterDroneHubV4);
					yield return typeof(JupiterDroneHubV5);
					yield return typeof(RadarSubwoofer);
					break;
				case "gemini":
					yield return typeof(GeminiCoreBooster);
					break;
				case "boat":
					yield return typeof(TideRunnerAnchorV2);
					break;
				default:
				{
					if (ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(shipKey)?.Configuration.ExclusiveArtifactTypes is { } exclusiveArtifactTypes)
						foreach (var exclusiveArtifactType in exclusiveArtifactTypes)
							yield return exclusiveArtifactType;
					break;
				}
			}
		}
	}

	private static void Artifact_GetTooltips_Postfix(Artifact __instance, ref List<Tooltip> __result)
	{
		if (__instance is not RippedPartArtifact artifact)
			return;

		var textTooltip = __result.OfType<TTText>().FirstOrDefault(t => t.text.StartsWith("<c=artifact>"));
		if (textTooltip is null)
			return;

		if (MG.inst.g?.state is not { } state || state.IsOutsideRun())
			return;
		textTooltip.text = DB.Join(
			"<c=artifact>{0}</c>\n".FF(__instance.GetLocName()),
			ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "artifact", "description", artifact.RippedPartType.ToString()])
		);
	}

	private sealed class AActuallySwapShipBody : CardAction
	{
		public required string NewShipKey;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var currentShipKey = s.ship.key;
			if (!StarterShip.ships.TryGetValue(NewShipKey, out var newShipStarter))
				return;
			if (!StarterShip.ships.TryGetValue(currentShipKey, out var currentShipStarter))
				return;

			var hullMaxDifference = Math.Max(s.ship.hullMax - currentShipStarter.ship.hullMax, 0);
			s.ship = Mutil.DeepCopy(newShipStarter.ship);
			s.ship.hullMax += hullMaxDifference;
			s.ship.hull += hullMaxDifference;
		}

		public override string GetUpgradeText(State s)
			=> ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Action-UpgradeText"]);
	}

	private sealed class ATakeCardsUntilSkip : CardAction
	{
		public required List<Card> Cards;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			s.GetCurrentQueue().InsertRange(0, Enumerable.Range(0, Cards.Count).Select(i => new ATakeCardsUntilSkipPart
			{
				Cards = Cards,
				ExpectedSkipped = i
			}));
		}
	}

	private sealed class ATakeCardsUntilSkipPart : CardAction
	{
		public required List<Card> Cards;
		public required int ExpectedSkipped;

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			timer = 0;
			var cardsLeft = Cards.Where(card => s.deck.All(cardInDeck => cardInDeck.uuid != card.uuid)).ToList();
			if (cardsLeft.Count != Cards.Count - ExpectedSkipped)
				return null;

			return new CardReward
			{
				cards = cardsLeft,
				canSkip = true
			};
		}
	}

	private sealed class ARipPart : CardAction
	{
		public required int PartToRipIndex;

		public override List<Tooltip> GetTooltips(State s)
		{
			var partType = s.ship.parts[PartToRipIndex].type;
			var hasMultiple = s.ship.parts.Where(p => p.type == partType).Skip(1).Any();
			return [new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::{nameof(ARipPart)}")
			{
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "AltAction-Tooltip-Base"]),
				Description = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "AltAction-Tooltip", partType.ToString(), hasMultiple ? "multiple" : "single"]),
			}];
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			s.ship.parts.RemoveAt(PartToRipIndex);
		}

		public override string GetUpgradeText(State s)
			=> ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "AltAction-UpgradeText"]);
	}
	
	private sealed class RippedPartArtifact : Artifact
	{
		public PType RippedPartType = PType.wing;
		
		public static void RegisterArtifact(IModHelper helper)
		{
			RippedPartArtifactEntry = helper.Content.Artifacts.RegisterArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					owner = Deck.colorless,
					pools = [ArtifactPool.EventOnly],
					unremovable = true
				},
				Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifact/RippedPart.png")).Sprite,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "ShipSwap", "artifact", "name"]).Localize,
				Description = ModEntry.Instance.AnyLocalizations.Bind(["event", "CombatDataCalibration", "artifact", "description", nameof(PType.wing)]).Localize
			});
		}

		public override List<Tooltip>? GetExtraTooltips()
			=> RippedPartType switch
			{
				PType.wing => StatusMeta.GetTooltips(Status.evade, 1),
				_ => null,
			};

		public override void OnReceiveArtifact(State state)
		{
			base.OnReceiveArtifact(state);

			switch (RippedPartType)
			{
				case PType.cockpit:
					state.ship.baseEnergy--;
					break;
				case PType.comms:
					state.ship.baseDraw--;
					break;
			}
		}

		public override void OnTurnEnd(State state, Combat combat)
		{
			base.OnTurnEnd(state, combat);
			
			if (RippedPartType == PType.wing)
				combat.Queue(new AStatus
				{
					targetPlayer = true,
					status = Status.evade,
					statusAmount = -1,
					artifactPulse = Key(),
				});
		}
	}

	private record struct EventRngData(
		List<string> NewShipKeys,
		int? PartToRipIndex
	);
}
