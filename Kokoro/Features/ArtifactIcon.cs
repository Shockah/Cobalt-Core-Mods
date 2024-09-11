using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public void RegisterArtifactIconHook(IArtifactIconHook hook, double priority)
		=> ArtifactIconManager.Instance.Register(hook, priority);

	public void UnregisterArtifactIconHook(IArtifactIconHook hook)
		=> ArtifactIconManager.Instance.Unregister(hook);
}

internal sealed class ArtifactIconManager : HookManager<IArtifactIconHook>
{
	internal static readonly ArtifactIconManager Instance = new();
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_Render_Transpiler))
		);
	}
	
	private static IEnumerable<CodeInstruction> Artifact_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_Render_Transpiler_CallManager)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Artifact_Render_Transpiler_CallManager(Artifact artifact, G g, Vec position)
		=> Instance.OnRenderArtifactIcon(g, artifact, position);
	
	internal void OnRenderArtifactIcon(G g, Artifact artifact, Vec position)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, g.state.EnumerateAllArtifacts()))
			hook.OnRenderArtifactIcon(g, artifact, position);
	}
}