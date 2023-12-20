using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class ArtifactPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Render)),
			transpiler: new HarmonyMethod(typeof(ArtifactPatches), nameof(Artifact_Render_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> Artifact_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Call("round"),
					ILMatches.Stloc<Vec>(originalMethod).CreateLdlocInstruction(out var ldlocPosition)
				)
				.Find(
					ILMatches.Ldarg(5),
					ILMatches.Brfalse
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocPosition,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactPatches), nameof(Artifact_Render_Transpiler_CallManager)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Artifact_Render_Transpiler_CallManager(Artifact artifact, G g, Vec position)
		=> Instance.ArtifactIconManager.OnRenderArtifactIcon(g, artifact, position);
}
