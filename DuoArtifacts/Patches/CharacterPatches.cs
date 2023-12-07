using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

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
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Render)),
			transpiler: new HarmonyMethod(typeof(CharacterPatches), nameof(Character_Render_Transpiler))
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

	private static IEnumerable<CodeInstruction> Character_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("tooltips"),
					ILMatches.Ldloc<Vec>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.CreateLdlocInstruction(out var ldlocPos)

				.Find(
					ILMatches.Call("AddGlossary"),
					ILMatches.Ldarg(10),
					ILMatches.Brfalse
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocPos,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CharacterPatches), nameof(Character_Render_Transpiler_AddDuoTooltips)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Character_Render_Transpiler_AddDuoTooltips(Character character, G g, Vec pos)
	{
		if (character.deckType is not { } deck || !ModEntry.IsEligibleForDuoArtifact(deck, g.state))
			return;
		g.tooltips.AddText(pos, I18n.CharacterEligibleForDuoArtifact);
	}
}
