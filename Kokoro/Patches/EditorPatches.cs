using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.Kokoro;

internal static class EditorPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredConstructor(typeof(Editor), []),
			postfix: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_ctor_Postfix))
		);
	}

	private static void Editor_ctor_Postfix(Editor __instance)
		=> __instance.allDecks = __instance.allDecks
			.Concat(DB.decks.Keys)
			.Distinct()
			.ToList();
}