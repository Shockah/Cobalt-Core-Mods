using HarmonyLib;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class CombatDataCalibrationEvent : IRegisterable
{
	private static string EventName = null!;
	private static IArtifactEntry ArtifactEntry = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		CombatAnalyzerGadgetArtifact.RegisterArtifact(helper);
		WeakCombatAnalyzerGadgetArtifact.RegisterArtifact(helper);

		EventName = $"{package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}";

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
		DB.story.all[$"{EventName}::Concerns"] = new()
		{
			type = NodeType.@event,
			lines = [
				new CustomSay
				{
					who = "scientist",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Concerns-1-Bjorn"]),
				},
				new CustomSay
				{
					who = "scientist",
					loopTag = "neutral",
					flipped = true,
					Text = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Concerns-2-Bjorn"]),
				},
			],
			choiceFunc = $"{EventName}::Concerns"
		};
		DB.story.all[$"{EventName}::Yes"] = new()
		{
			type = NodeType.@event,
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
		DB.eventChoiceFns[$"{EventName}::Concerns"] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetConcernsChoices));
	}

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		var node = DB.story.all[EventName];
		node.never = settings.DisabledEvents.Contains(MoreEvent.CombatDataCalibration) ? true : null;
		node.dontCountForProgression = settings.DisabledEvents.Contains(MoreEvent.CombatDataCalibration);
		ArtifactEntry.Configuration.Meta.pools = ArtifactEntry.Configuration.Meta.pools
			.Where(p => p != ArtifactPool.Unreleased)
			.Concat(settings.DisabledEvents.Contains(MoreEvent.CombatDataCalibration) ? [ArtifactPool.Unreleased] : [])
			.ToArray();
	}

	[UsedImplicitly]
	private static List<Choice> GetChoices(State state)
		=> [
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Choice-Yes"]),
				key = $"{EventName}::Yes",
				actions = [
					new AAddArtifact { artifact = new CombatAnalyzerGadgetArtifact() }
				]
			},
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Choice-Concerns"]),
				key = $"{EventName}::Concerns"
			},
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Choice-No"]),
				key = $"{EventName}::No"
			},
		];

	[UsedImplicitly]
	private static List<Choice> GetConcernsChoices(State state)
		=> [
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Concerns-Choice-Yes"]),
				key = $"{EventName}::Yes",
				actions = [
					new AAddArtifact { artifact = new WeakCombatAnalyzerGadgetArtifact() }
				]
			},
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Concerns-Choice-Science"]),
				key = $"{EventName}::Yes",
				actions = [
					new AAddArtifact { artifact = new CombatAnalyzerGadgetArtifact() }
				]
			},
			new()
			{
				label = ModEntry.Instance.Localizations.Localize(["event", "CombatDataCalibration", "Concerns-Choice-No"]),
				key = $"{EventName}::No"
			},
		];

	private sealed class CombatAnalyzerGadgetArtifact : Artifact
	{
		public static void RegisterArtifact(IModHelper helper)
		{
			ArtifactEntry = helper.Content.Artifacts.RegisterArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					owner = Deck.colorless,
					pools = [ArtifactPool.EventOnly],
					unremovable = true
				},
				Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifact/CombatAnalyzerGadget.png")).Sprite,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "CombatDataCalibration", "artifact", "name"]).Localize,
				Description = ModEntry.Instance.AnyLocalizations.Bind(["event", "CombatDataCalibration", "artifact", "description"]).Localize
			});
		}

		public override List<Tooltip> GetExtraTooltips()
			=> StatusMeta.GetTooltips(Status.powerdrive, 1);

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
			if (state.map.markers[state.map.currentLocation].contents is not MapBattle { battleType: BattleType.Boss })
				return;

			state.rewardsQueue.Queue(new ALoseArtifact { artifactType = Key() });
			state.rewardsQueue.Queue(new AArtifactOffering
			{
				amount = 3,
				limitPools = [ArtifactPool.Boss]
			});
		}
	}
	
	private sealed class WeakCombatAnalyzerGadgetArtifact : Artifact
	{
		public static void RegisterArtifact(IModHelper helper)
		{
			ArtifactEntry = helper.Content.Artifacts.RegisterArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
			{
				ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
				Meta = new()
				{
					owner = Deck.colorless,
					pools = [ArtifactPool.EventOnly],
					unremovable = true
				},
				Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifact/WeakCombatAnalyzerGadget.png")).Sprite,
				Name = ModEntry.Instance.AnyLocalizations.Bind(["event", "CombatDataCalibration", "artifactWeak", "name"]).Localize,
				Description = ModEntry.Instance.AnyLocalizations.Bind(["event", "CombatDataCalibration", "artifactWeak", "description"]).Localize
			});
		}

		public override List<Tooltip> GetExtraTooltips()
			=> StatusMeta.GetTooltips(Status.overdrive, 1);

		public override void OnTurnStart(State state, Combat combat)
		{
			base.OnTurnStart(state, combat);
			if (combat.turn != 1)
				return;
			
			combat.Queue(new AStatus
			{
				targetPlayer = false,
				status = Status.overdrive,
				statusAmount = 1,
				artifactPulse = Key()
			});
		}

		public override void OnCombatEnd(State state)
		{
			base.OnCombatEnd(state);
			if (state.map.markers[state.map.currentLocation].contents is not MapBattle { battleType: BattleType.Boss })
				return;

			state.rewardsQueue.Queue(new ALoseArtifact { artifactType = Key() });
			state.rewardsQueue.Queue(new AArtifactOffering
			{
				amount = 3,
				limitPools = [ArtifactPool.Common]
			});
		}
	}
}
