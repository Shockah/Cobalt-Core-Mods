using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class IntroStory : BaseStory, IRegisterable
{
	private static ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations = null!;
	private static ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations = null!;
	
	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/Story/Intro.{locale}.json").OpenRead()
		);
		Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
		);
		
		var ghost = ModEntry.Instance.GhostCharacter.CharacterType;
		var dizzy = Deck.dizzy.Key();
		const string cat = "comp";
		
		var mainName = $"{package.Manifest.UniqueName}::Intro";
		DB.story.all[$"{mainName}Initial"] = new()
		{
			type = NodeType.@event,
			once = true,
			lines = [
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "0" },
				new Say { who = cat, loopTag = "grumpy", hash = "1" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "2" },
				new Say { who = cat, loopTag = "squint", hash = "3" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "4" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "5" },
				new Say { who = cat, loopTag = "grumpy", hash = "6" },
				new Say { who = cat, loopTag = "squint", hash = "7" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "8" },
				new Say { who = cat, loopTag = "neutral", hash = "9" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "10" },
				new Say { who = cat, loopTag = "squint", hash = "11" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "12" },
			],
		};
		DB.story.all[$"{mainName}Dizzy1"] = new()
		{
			type = NodeType.@event,
			once = true,
			priority = true,
			LookupAny = ["zone_first", "zone_lawless", "zone_third"],
			requiredScenes = ["Peri_Memory_3"],
			allPresent = [dizzy],
			lines = [
				new Say { who = dizzy, loopTag = "intense", hash = "0" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "1" },
				new Say { who = dizzy, loopTag = "serious", hash = "2" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "3" },
				new Say { who = dizzy, loopTag = "intense", hash = "4" },
				new Say { who = dizzy, loopTag = "serious", hash = "5" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "6" },
				new Say { who = dizzy, loopTag = "neutral", hash = "7" },
				new Say { who = cat, loopTag = "squint", hash = "8" },
				new Say { who = dizzy, loopTag = "shrug", hash = "9" },
				new Say { who = ghost, loopTag = "neutral", flipped = true, hash = "10" },
			],
		};

		InjectLocalizations(helper, localizations =>
		{
			for (var i = 0; i <= 12; i++)
				localizations[$"{mainName}Initial:{i}"] = Localizations.Localize(["Initial", i.ToString()]);
			for (var i = 0; i <= 10; i++)
				localizations[$"{mainName}Dizzy1:{i}"] = Localizations.Localize(["Dizzy1", i.ToString()]);
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Dialogue), nameof(Dialogue.MakeNextRoute)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Dialogue_MakeNextRoute_Prefix))
		);
	}

	private static bool Dialogue_MakeNextRoute_Prefix(State s, ref OnDone? onDone, ref Route? __result)
	{
		if (onDone != OnDone.visitCurrent)
			return true;
		if (s.storyVars.visitedNodes.Contains($"{ModEntry.Instance.Package.Manifest.UniqueName}::IntroInitial"))
			return true;

		__result = Dialogue.MakeDialogueRouteOrSkip(s, DB.story.QuickLookup(s, $"{ModEntry.Instance.Package.Manifest.UniqueName}::IntroInitial"), onDone);
		return false;
	}
}