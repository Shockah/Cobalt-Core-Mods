using HarmonyLib;
using Shockah.Shared;
using System;

namespace Shockah.DuoArtifacts;

internal static class CharacterPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Character), nameof(Character.GetDisplayName), new Type[] { typeof(string), typeof(State) }),
			postfix: new HarmonyMethod(typeof(CharacterPatches), nameof(Character_GetDisplayName_Postfix))
		);
	}

	private static void Character_GetDisplayName_Postfix(string charId, ref string __result)
	{
		if (charId == Instance.Database.DuoArtifactDeck.GlobalName)
			__result = I18n.DuoArtifactDeckName;
		else if (charId == Instance.Database.TrioArtifactDeck.GlobalName)
			__result = I18n.TrioArtifactDeckName;
		else if (charId == Instance.Database.ComboArtifactDeck.GlobalName)
			__result = I18n.ComboArtifactDeckName;
	}
}
