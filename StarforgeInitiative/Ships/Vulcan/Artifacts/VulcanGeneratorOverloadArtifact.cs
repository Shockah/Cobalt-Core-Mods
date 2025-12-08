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

internal sealed class VulcanGeneratorOverloadArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("VulcanGeneratorOverload", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Vulcan/Artifact/GeneratorOverload.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Vulcan", "artifact", "GeneratorOverload", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Vulcan", "artifact", "GeneratorOverload", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("cardtrait.exhaust"),
			new TTGlossary("cardtrait.unplayable"),
		];
	
	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var didAllowExpensivePlayAndShouldEndTurnLocal = il.DeclareLocal(typeof(bool));
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod),
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
					new CodeInstruction(OpCodes.Ldarg_0),
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
		if (state.EnumerateAllArtifacts().OfType<VulcanGeneratorOverloadArtifact>().FirstOrDefault() is not { } artifact)
			return false;

		// TODO: skip this check with a boss artifact
		if (cost - combat.energy > 1)
			return false;

		cost = combat.energy;
		actuallyExhaust = false;
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.Helper.Content.Cards.UnplayableCardTrait, true, false);
		
		artifact.Pulse();
		return true;
	}

	private static void Combat_TryPlayCard_Transpiler_EndTurnIfNeeded(Combat combat, bool didAllowExpensivePlayAndShouldEndTurn)
	{
		if (!didAllowExpensivePlayAndShouldEndTurn)
			return;
		combat.Queue(new AEndTurn());
	}
}