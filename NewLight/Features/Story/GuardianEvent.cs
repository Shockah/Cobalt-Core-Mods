using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class GuardianEventStory : BaseStory, IRegisterable
{
	private static ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations = null!;
	private static ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations = null!;
	
	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/Story/GuardianEvent.{locale}.json").OpenRead()
		);
		Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
		);
		
		var ghost = ModEntry.Instance.GhostCharacter.CharacterType;

		var mainName = $"{package.Manifest.UniqueName}::GuardianEvent";
		DB.story.all[$"{mainName}Initial"] = new()
		{
			type = NodeType.@event,
			lookup = [mainName],
			zones = ["zone_first"],
			requiredScenes = [$"{package.Manifest.UniqueName}::IntroInitial"],
			oncePerRun = true,
			lines = [
				new Say { who = ghost, loopTag = "neutral", hash = "0" },
			],
			choiceFunc = mainName,
		};
		DB.story.all[$"{mainName}Infinite"] = new()
		{
			type = NodeType.@event,
			lookup = [mainName],
			zones = ["zone_lawless", "zone_three"],
			requiredScenes = [$"{mainName}Initial"],
			lines = [
				new Say { who = ghost, loopTag = "neutral", hash = "0" },
			],
			choiceFunc = mainName,
		};
		DB.eventChoiceFns[mainName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
		
		InjectLocalizations(helper, localizations =>
		{
			localizations[$"{mainName}Initial:0"] = Localizations.Localize(["Initial", "0"]);
			localizations[$"{mainName}Infinite:0"] = Localizations.Localize(["Infinite", "0"]);
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Dialogue), nameof(Dialogue.MakeNextRoute)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Dialogue_MakeNextRoute_Prefix))
		);
	}

	[UsedImplicitly]
	private static List<Choice> GetChoices(State state)
		=> [
			.. state.characters
				.Select(character => character.deckType)
				.WhereNotNull()
				.Select(deck => new Choice
				{
					label = $"<c={deck.Key()}>{Character.GetDisplayName(deck, state)}</c>",
					actions = [new SingleAction { Who = deck }],
				}),
			new()
			{
				label = Localizations.Localize(["Choice", "Share", "Name"]),
				actions = [new ShareAction()],
			},
			new()
			{
				label = Localizations.Localize(["Choice", "NoOne", "Name"]),
				actions = state.EnumerateAllArtifacts().Any(a => a is GhostArtifact) ? [new NoOneAction()] : [],
			},
		];

	private static bool Dialogue_MakeNextRoute_Prefix(State s, ref OnDone? onDone, ref Route? __result)
	{
		if (onDone != OnDone.visitCurrent)
			return true;
		if (s.map.GuardianEventDone)
			return true;

		var guardianEventStoryNode = DB.story.QuickLookup(s, $".{ModEntry.Instance.Package.Manifest.UniqueName}::GuardianEvent");
		if (guardianEventStoryNode is null)
			return true;

		s.map.GuardianEventDone = true;
		__result = Dialogue.MakeDialogueRouteOrSkip(s, guardianEventStoryNode, onDone);
		return false;
	}

	private sealed class SingleAction : CardAction
	{
		public required Deck Who;

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::GuardianEvent::Guardian")
				{
					TitleColor = Colors.white,
					Title = Localizations.Localize(["Choice", "Single", "Action", "Name"]),
					Description = Localizations.Localize(["Choice", "Single", "Action", "Description"]),
				}
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			if (s.characters.Any(character => character.deckType == Who && character.artifacts.Any(a => a is GhostArtifact)))
				return;
			
			s.GetCurrentQueue().AddRange([
				new ALoseArtifact { artifactType = GhostArtifact.Entry.UniqueName },
				new AAddArtifact { artifact = new GhostArtifact(), timer = 0 },
				new MoveArtifactAction { Who = Who },
			]);
		}

		private sealed class MoveArtifactAction : CardAction
		{
			public required Deck Who;

			public override void Begin(G g, State s, Combat c)
			{
				base.Begin(g, s, c);
				for (var i = 0; i < s.artifacts.Count; i++)
				{
					if (s.artifacts[i] is not GhostArtifact artifact)
						continue;
					
					s.artifacts.RemoveAt(i);
					foreach (var character in s.characters)
					{
						if (character.deckType != Who)
							continue;
						character.artifacts.Add(artifact);
						break;
					}
					break;
				}
			}
		}
	}

	private sealed class ShareAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::GuardianEvent::Guardian")
				{
					TitleColor = Colors.white,
					Title = Localizations.Localize(["Choice", "Share", "Action", "Name"]),
					Description = Localizations.Localize(["Choice", "Share", "Action", "Description"]),
				}
			];
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.artifacts.Any(a => a is GhostArtifact))
				return;
			
			s.GetCurrentQueue().AddRange([
				new ALoseArtifact { artifactType = GhostArtifact.Entry.UniqueName },
				new AAddArtifact { artifact = new GhostArtifact() },
			]);
		}
	}

	private sealed class NoOneAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::GuardianEvent::Guardian")
				{
					TitleColor = Colors.white,
					Title = Localizations.Localize(["Choice", "NoOne", "Action", "Name"]),
					Description = Localizations.Localize(["Choice", "NoOne", "Action", "Description"]),
				}
			];
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			s.GetCurrentQueue().Add(new ALoseArtifact { artifactType = GhostArtifact.Entry.UniqueName });
		}
	}
}

file static class GuardianEventExt
{
	extension(MapBase map)
	{
		public bool GuardianEventDone
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(map, "GuardianEventDone");
			set => ModEntry.Instance.Helper.ModData.SetModData(map, "GuardianEventDone", value);
		}
	}
}