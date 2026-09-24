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
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/Story/Intro.{locale}.json").OpenRead()
		);
		Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
		);
		
		var ghost = ModEntry.Instance.GhostCharacter.CharacterType;
		const string cat = "comp";
		
		DB.story.all[$"{package.Manifest.UniqueName}::Intro"] = new()
		{
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

		InjectLocalizations(helper, localizations =>
		{
			for (var i = 0; i <= 12; i++)
				localizations[$"{package.Manifest.UniqueName}::Intro:{i}"] = Localizations.Localize(["Lines", i.ToString()]);
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
		if (s.storyVars.visitedNodes.Contains($"{ModEntry.Instance.Package.Manifest.UniqueName}::Intro"))
			return true;

		__result = Dialogue.MakeDialogueRouteOrSkip(s, DB.story.QuickLookup(s, $"{ModEntry.Instance.Package.Manifest.UniqueName}::Intro"), onDone);
		return false;
	}
}