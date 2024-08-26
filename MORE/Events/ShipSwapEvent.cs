using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class ShipSwapEvent : IRegisterable
{
	private static string EventName = null!;

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
			oncePerRun = true,
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
		DB.story.all[$"{EventName}::No"] = new()
		{
			type = NodeType.@event,
			oncePerRun = true,
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
	}

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		var node = DB.story.all[EventName];
		node.never = settings.DisabledEvents.Contains(MoreEvent.ShipSwap) ? true : null;
		node.dontCountForProgression = settings.DisabledEvents.Contains(MoreEvent.ShipSwap);
	}

	private static List<Choice> GetChoices(State state)
	{
		var currentShipKey = state.ship.key;
		var otherShips = StarterShip.ships.Keys
			.Where(key => currentShipKey != key)
			.ToList();
		var newShipKeys = otherShips.Shuffle(state.rngCurrentEvent).Take(3).ToList();

		return Enumerable.Range(0, newShipKeys.Count)
			.Select(i => new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-Yes", i.ToString()], new { ShipName = Loc.T($"ship.{newShipKeys[i]}.name") }),
				key = $"{EventName}::Yes",
				actions = [new ASwapShip { NewShipKey = newShipKeys[i] }]
			})
			.Append(new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-No"]),
				key = $"{EventName}::No"
			})
			.ToList();
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

				var exclusiveArtifacts = GetExclusiveArtifactTypes(NewShipKey)
					.Select(t => (Artifact)Activator.CreateInstance(t)!)
					.Where(a => !a.GetMeta().pools.Contains(ArtifactPool.Unreleased))
					.Shuffle(s.rngCurrentEvent)
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
					{
						foreach (var exclusiveArtifactType in exclusiveArtifactTypes)
							yield return exclusiveArtifactType;
					}
					break;
				}
			}
		}
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

		public override string? GetUpgradeText(State s)
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
}
