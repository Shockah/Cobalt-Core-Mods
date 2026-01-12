using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.StarforgeInitiative;

internal sealed class FlickerBorrow : IRegisterable
{
	internal static IStatusEntry AfterflickStatus { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AfterflickStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Afterflick", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Flicker/Status/Afterflick.png")).Sprite,
				color = new("BE4611"),
				isGood = false,
				affectedByTimestop = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "status", "Afterflick", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Flicker", "status", "Afterflick", "description"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderingHook());
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<int>(originalMethod).GetLocalIndex(out var costLocalIndex).ExtractLabels(out var labels),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(Combat.energy)),
					ILMatches.Bgt,
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldloca, costLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_AllowExpensivePlay))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Combat_TryPlayCard_Transpiler_AllowExpensivePlay(State state, Combat combat, ref int cost)
	{
		if (combat.energy >= cost)
			return;
		if (state.ship.Get(AfterflickStatus.Status) > 0)
			return;
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is FlickerFleetingCoreArtifact) is not { } artifact)
			return;

		var missingEnergy = cost - combat.energy;
		var allowedToBorrow = 1 - state.ship.Get(Status.energyLessNextTurn);

		if (missingEnergy > allowedToBorrow)
			return;

		state.ship.Add(Status.energyLessNextTurn, missingEnergy);
		combat.energy += missingEnergy;
		artifact.Pulse();
	}

	private sealed class StatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
			=> args.Status == AfterflickStatus.Status ? ModEntry.Instance.KokoroApi.StatusRendering.EmptyStatusInfoRenderer : null;
	}
}