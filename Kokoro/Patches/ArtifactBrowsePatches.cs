using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class ArtifactBrowsePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly Lazy<Func<ArtifactBrowse, Dictionary<UIKey, double>>> ArtifactToScrollYCacheGetter = new(() => AccessTools.DeclaredField(typeof(ArtifactBrowse), "artifactToScrollYCache").EmitInstanceGetter<ArtifactBrowse, Dictionary<UIKey, double>>());

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.Render)),
			transpiler: new HarmonyMethod(typeof(ArtifactBrowsePatches), nameof(ArtifactBrowse_Render_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> ArtifactBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("ReadScrollInputAndUpdate"))
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.Before, ILMatches.LdcI4(280))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactBrowsePatches), nameof(ArtifactBrowse_Render_Transpiler_ModifyScrollLength)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static int ArtifactBrowse_Render_Transpiler_ModifyScrollLength(int scrollLength, ArtifactBrowse menu)
	{
		var scrollYCache = ArtifactToScrollYCacheGetter.Value(menu);
		if (scrollYCache.Count == 0)
			return scrollLength;
		return (int)scrollYCache.Values.Max() - 150;
	}
}
