using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;

namespace Shockah.Soggins;

internal static class StoryVarsExt
{
	extension(StoryVars vars)
	{
		public bool DidBotchCard
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(vars, "DidBotchCard");
			set => ModEntry.Instance.Helper.ModData.SetModData(vars, "DidBotchCard", value);
		}
		
		public bool DidDoubleCard
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(vars, "DidDoubleCard");
			set => ModEntry.Instance.Helper.ModData.SetModData(vars, "DidDoubleCard", value);
		}
		
		public bool DidDoubleLaunchAction
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(vars, "DidDoubleLaunchAction");
			set => ModEntry.Instance.Helper.ModData.SetModData(vars, "DidDoubleLaunchAction", value);
		}
	}
}

internal sealed class NarrativeManager
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.ResetAfterCombatLine)),
			postfix: new HarmonyMethod(typeof(NarrativeManager), nameof(StoryVars_ResetAfterCombatLine_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Narrative), nameof(Narrative.PickWhenActionsAreDone)),
			transpiler: new HarmonyMethod(typeof(NarrativeManager), nameof(Narrative_PickWhenActionsAreDone_Transpiler))
		);
	}

	private static void StoryVars_ResetAfterCombatLine_Postfix(StoryVars __instance)
	{
		__instance.DidBotchCard = false;
		__instance.DidDoubleCard = false;
		__instance.DidDoubleLaunchAction = false;
	}

	private static IEnumerable<CodeInstruction> Narrative_PickWhenActionsAreDone_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("state"),
					new ElementMatch<CodeInstruction>("ldfld `storyVars` or ldfld `persistentStoryVars`", i => ILMatches.Ldfld("storyVars").Matches(i) || ILMatches.Ldfld("persistentStoryVars").Matches(i)),
					ILMatches.Ldfld("skipTutorial"),
					ILMatches.Brtrue.GetBranchTarget(out var branchTarget)
				)
				.PointerMatcher(branchTarget)
				.Find(ILMatches.Brtrue.GetBranchTarget(out branchTarget))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NarrativeManager), nameof(Narrative_PickWhenActionsAreDone_Transpiler_OverrideNarrative))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool Narrative_PickWhenActionsAreDone_Transpiler_OverrideNarrative(G g, Combat combat)
	{
		if (combat.lowPriorityCooldown > 0)
			return false;
		
		var deck = g.state.storyVars.whoDidThat ?? Deck.colorless;

		if (g.state.storyVars.DidBotchCard)
		{
			double? responseDelay = DB.story.QuickLookup(g.state, $".{Instance.SogginsDeck.GlobalName}_Botch") is null ? null : 1.25;
			Narrative.SpeakBecauseOfAction(g, combat, $".{Instance.SogginsDeck.GlobalName}_Botch");
			QueuedAction.Queue(combat, new BotchCardResponseQueuedAction
			{
				WaitForTotalGameTime = responseDelay is null ? null : MG.inst.g.time + responseDelay.Value,
				Deck = deck,
			});
			return true;
		}
		if (g.state.storyVars.DidDoubleCard)
		{
			var wasLaunchAction = g.state.storyVars.DidDoubleLaunchAction;
			double? responseDelay = DB.story.QuickLookup(g.state, $".{Instance.SogginsDeck.GlobalName}_Double") is null ? null : 1.25;
			Narrative.SpeakBecauseOfAction(g, combat, $".{Instance.SogginsDeck.GlobalName}_Double");

			QueuedAction queuedAction = wasLaunchAction ? new DoubleLaunchResponseQueuedAction() : new DoubleResponseQueuedAction();
			queuedAction.WaitForTotalGameTime = responseDelay is null ? null : MG.inst.g.time + responseDelay.Value;
			QueuedAction.Queue(combat, queuedAction);
			return true;
		}

		return false;
	}

	private sealed class BotchCardResponseQueuedAction : QueuedAction
	{
		public required Deck Deck;

		protected override void Begin(G g, State state, Combat combat)
		{
			var deckKey = Deck == Deck.colorless ? "comp" : Deck.Key();
			Narrative.SpeakBecauseOfAction(g, combat, $".{Instance.SogginsDeck.GlobalName}_BotchResponse_{deckKey}");
		}
	}

	private sealed class DoubleResponseQueuedAction : QueuedAction
	{
		protected override void Begin(G g, State state, Combat combat)
			=> Narrative.SpeakBecauseOfAction(g, combat, $".{Instance.SogginsDeck.GlobalName}_DoubleResponse");
	}

	private sealed class DoubleLaunchResponseQueuedAction : QueuedAction
	{
		protected override void Begin(G g, State state, Combat combat)
		{
			var storyKey = $".{Instance.SogginsDeck.GlobalName}_DoubleLaunchResponse";
			if (DB.story.QuickLookup(g.state, storyKey) is null)
				storyKey = $".{Instance.SogginsDeck.GlobalName}_DoubleResponse";
			Narrative.SpeakBecauseOfAction(g, combat, storyKey);
		}
	}
}
