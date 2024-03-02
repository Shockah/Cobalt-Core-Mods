using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.EventsGalore;

internal sealed class CombatDataCalibrationEvent : IRegisterable
{
	private static string EventName = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		GadgetArtifact.Register(package, helper);

		EventName = $"{package.Manifest.UniqueName}::{typeof(CombatDataCalibrationEvent).Name}";

		DB.story.all[EventName] = new()
		{
			type = NodeType.@event,
			canSpawnOnMap = true,
			oncePerRun = true,
			zones = ["zone_first", "zone_lawless"],
			lines = [
				new CustomSay
				{
					who = "comp",
					loopTag = "neutral",
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "1-CAT"])
				},
				new CustomSay
				{
					who = "scientist",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "2-Bjorn"])
				},
				new CustomSay
				{
					who = "scientist",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "3-Bjorn"])
				},
				new CustomSay
				{
					who = "scientist",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "4-Bjorn"])
				},
			],
			choiceFunc = EventName
		};
		DB.story.all[$"{EventName}::Yes"] = new()
		{
			type = NodeType.@event,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "scientist",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Yes-1-Bjorn"])
				},
			]
		};
		DB.story.all[$"{EventName}::No"] = new()
		{
			type = NodeType.@event,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "scientist",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "No-1-Bjorn"])
				},
			]
		};

		DB.eventChoiceFns[EventName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
	}

	private static List<Choice> GetChoices(State state)
		=> [
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Choice-Yes"]),
				key = $"{EventName}::Yes",
				actions = [
					new AAddArtifact { artifact = new GadgetArtifact() }
				]
			},
			new Choice
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Choice-No"]),
				key = $"{EventName}::No"
			}
		];

	private sealed class GadgetArtifact : Artifact, IRegisterable
	{
		public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
		{
			helper.Content.Artifacts.RegisterArtifact("CombatAnalyzerGadget", new()
			{
				ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					owner = Deck.colorless,
					pools = [ArtifactPool.EventOnly],
					unremovable = true
				},
				Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/CombatAnalyzerGadget.png")).Sprite,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "CombatDataCalibration", "artifact", "name"]).Localize,
				Description = ModEntry.Instance.AnyLocalizations.Bind(["event", "CombatDataCalibration", "artifact", "description"]).Localize
			});
		}

		public override void OnCombatStart(State state, Combat combat)
		{
			base.OnCombatStart(state, combat);
			combat.Queue(new AStatus
			{
				targetPlayer = false,
				status = Status.powerdrive,
				statusAmount = 1,
				artifactPulse = Key()
			});
		}

		public override void OnCombatEnd(State state)
		{
			base.OnCombatEnd(state);
			if (state.map.markers[state.map.currentLocation].contents is not MapBattle mb)
				return;

			state.rewardsQueue.Queue(new ALoseArtifact { artifactType = Key() });
			state.rewardsQueue.Queue(new AArtifactOffering
			{
				amount = 3,
				limitPools = [ArtifactPool.Boss]
			});
		}
	}
}
