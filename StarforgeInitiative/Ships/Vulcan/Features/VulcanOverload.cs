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

namespace Shockah.StarforgeInitiative;

internal sealed class VulcanOverload : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var didAllowExpensivePlayAndShouldEndTurnLocal = il.DeclareLocal(typeof(bool));
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod).GetLocalIndex(out var dataLocalIndex),
					ILMatches.Ldfld(nameof(CardData.exhaust)),
					ILMatches.Ldarg(4),
					ILMatches.Instruction(OpCodes.Or),
					ILMatches.Stloc<bool>(originalMethod).GetLocalIndex(out var actuallyExhaustLocalIndex),
				])
				.Find([
					ILMatches.Ldloc<int>(originalMethod).GetLocalIndex(out var costLocalIndex).ExtractLabels(out var labels),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(Combat.energy)),
					ILMatches.Bgt,
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldloca, costLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloca, actuallyExhaustLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_AllowExpensivePlay))),
					new CodeInstruction(OpCodes.Stloc, didAllowExpensivePlayAndShouldEndTurnLocal),
				])
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldloc<List<CardAction>>(originalMethod),
				])
				.Find(ILMatches.Call(nameof(Combat.Queue)))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldloc, dataLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, didAllowExpensivePlayAndShouldEndTurnLocal),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_EndTurnIfNeeded))),
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

	private static bool Combat_TryPlayCard_Transpiler_AllowExpensivePlay(State state, Combat combat, Card card, ref int cost, ref bool actuallyExhaust)
	{
		if (combat.energy >= cost)
			return false;
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is VulcanGeneratorOverloadArtifact) is not { } artifact)
			return false;

		var missingEnergy = cost - combat.energy;
		if (missingEnergy > 1)
		{
			if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is VulcanUpgradedCapacitorsArtifact) is { } capacitorsArtifact)
				capacitorsArtifact.Pulse();
			else
				return false;
		}

		cost = 0;
		actuallyExhaust = false;
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.Helper.Content.Cards.UnplayableCardTrait, true, false);
		
		artifact.Pulse();
		return true;
	}

	private static void Combat_TryPlayCard_Transpiler_EndTurnIfNeeded(State state, Combat combat, CardData data, bool didAllowExpensivePlayAndShouldEndTurn)
	{
		if (!didAllowExpensivePlayAndShouldEndTurn)
			return;

		if (data.singleUse)
			combat.Queue(new AAddCard { card = new TrashUnplayable(), destination = CardDestination.Discard });

		if (state.EnumerateAllArtifacts().OfType<VulcanWaterCoolingArtifact>().FirstOrDefault() is { TriggeredThisTurn: false } artifact)
		{
			artifact.TriggeredThisTurn = true;
			artifact.Pulse();
		}
		else
		{
			combat.Queue(new AEndTurn());
		}
	}
}