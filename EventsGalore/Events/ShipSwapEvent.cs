using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.EventsGalore;

internal sealed class ShipSwapEvent : IRegisterable
{
	private static string EventName = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		EventName = $"{package.Manifest.UniqueName}::{typeof(ShipSwapEvent).Name}";

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

	private static List<Choice> GetChoices(State state)
		=> [
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-Yes"]),
				key = $"{EventName}::Yes",
				actions = [
					new AShipUpgrades
					{
						actions = [
							new AChangeShipRandomly()
						]
					}
				]
			},
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Choice-No"]),
				key = $"{EventName}::No"
			}
		];

	private sealed class AChangeShipRandomly : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var otherShips = StarterShip.ships.Keys
				.Where(key => s.ship.key != key)
				.ToList();
			var newShipKey = otherShips.Random(s.rngCurrentEvent);

			if (newShipKey is null)
				return;
			if (!StarterShip.ships.TryGetValue(newShipKey, out var newShipStarter))
				return;
			if (!StarterShip.ships.TryGetValue(s.ship.key, out var currentShipStarter))
				return;

			var currentShip = s.ship;
			s.ship = Mutil.DeepCopy(newShipStarter.ship);
			s.ship.shardMaxBase = currentShip.shardMaxBase;
			s.ship.heatMin = currentShip.heatMin;
			s.ship.heatTrigger = currentShip.heatTrigger;

			List<CardAction> moreActions = [];

			foreach (var artifact in currentShipStarter.artifacts)
				if (s.EnumerateAllArtifacts().Any(a => a.Key() == artifact.Key()))
					moreActions.Add(new ALoseArtifact { artifactType = artifact.Key() });
			foreach (var artifactType in GetExclusiveArtifactTypes(s.ship.key))
				if (s.EnumerateAllArtifacts().FirstOrDefault(a => a.GetType() == artifactType) is { } artifact)
					moreActions.Add(new ALoseArtifact { artifactType = artifact.Key() });
			foreach (var artifact in newShipStarter.artifacts)
				moreActions.Add(new AAddArtifact { artifact = Mutil.DeepCopy(artifact) });

			moreActions.Add(new ATakeCardsUntilSkip { Cards = newShipStarter.cards.Select(card => card.CopyWithNewId()).ToList() });
			s.GetCurrentQueue().InsertRange(0, moreActions);
		}

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTText(ModEntry.Instance.Localizations.Localize(["event", "ShipSwap", "Action-Tooltip"]))];

		private static IEnumerable<Type> GetExclusiveArtifactTypes(string shipKey)
		{
			if (shipKey == "artemis")
			{
				yield return typeof(HunterWings);
			}
			else if (shipKey == "ares")
			{
				yield return typeof(AresCannonV2);
				yield return typeof(ControlRodsV2);
			}
			else if (shipKey == "jupiter")
			{
				yield return typeof(JupiterDroneHubV2);
				yield return typeof(JupiterDroneHubV3);
				yield return typeof(JupiterDroneHubV4);
				yield return typeof(JupiterDroneHubV5);
				yield return typeof(RadarSubwoofer);
			}
			else if (shipKey == "gemini")
			{
				yield return typeof(GeminiCoreBooster);
			}
			else if (shipKey == "boat")
			{
				yield return typeof(TideRunnerAnchorV2);
			}
			else if (ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(shipKey)?.Configuration.ExclusiveArtifactTypes is { } exclusiveArtifactTypes)
			{
				foreach (var exclusiveArtifactType in exclusiveArtifactTypes)
					yield return exclusiveArtifactType;
			}
		}
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
			var cardsLeft = Cards.Where(card => !s.deck.Any(cardInDeck => cardInDeck.uuid == card.uuid)).ToList();
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
