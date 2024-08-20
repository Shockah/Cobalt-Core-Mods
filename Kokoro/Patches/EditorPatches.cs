using HarmonyLib;
using Nickel;
using System.Linq;

namespace Shockah.Kokoro;

// ReSharper disable InconsistentNaming
internal static class EditorPatches
{
	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredConstructor(typeof(Editor), []),
			postfix: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_ctor_Postfix))
		);
	}

	private static void Editor_ctor_Postfix(Editor __instance)
		=> __instance.allDecks = __instance.allDecks
			.Concat(DB.decks.Keys)
			.Distinct()
			.ToList();
}