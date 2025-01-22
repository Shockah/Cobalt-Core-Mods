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

internal sealed class NarrativeManager
{
	private static ModEntry Instance => ModEntry.Instance;

	public bool DidBotchCard;
	public bool DidDoubleCard;
	public bool DidDoubleLaunchAction;

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

	private static void StoryVars_ResetAfterCombatLine_Postfix()
	{
		var manager = Instance.NarrativeManager;
		manager.DidBotchCard = false;
		manager.DidDoubleCard = false;
		manager.DidDoubleLaunchAction = false;
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
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool Narrative_PickWhenActionsAreDone_Transpiler_OverrideNarrative(G g, Combat combat)
	{
		if (combat.lowPriorityCooldown > 0)
			return false;
		
		var manager = Instance.NarrativeManager;
		var deck = g.state.storyVars.whoDidThat ?? Deck.colorless;
		var deckKey = deck == Deck.colorless ? "comp" : deck.Key();

		if (manager.DidBotchCard)
		{
			double? responseDelay = DB.story.QuickLookup(g.state, $".{Instance.SogginsDeck.GlobalName}_Botch") is null ? null : 1.25;
			Narrative.SpeakBecauseOfAction(g, combat, $".{Instance.SogginsDeck.GlobalName}_Botch");
			QueuedAction.Queue(new QueuedAction
			{
				WaitForTotalGameTime = responseDelay is null ? null : MG.inst.g.time + responseDelay.Value,
				Action = () => Narrative.SpeakBecauseOfAction(g, combat, $".{Instance.SogginsDeck.GlobalName}_BotchResponse_{deckKey}")
			});
			return true;
		}
		if (manager.DidDoubleCard)
		{
			var wasLaunchAction = manager.DidDoubleLaunchAction;
			double? responseDelay = DB.story.QuickLookup(g.state, $".{Instance.SogginsDeck.GlobalName}_Double") is null ? null : 1.25;
			Narrative.SpeakBecauseOfAction(g, combat, $".{Instance.SogginsDeck.GlobalName}_Double");
			QueuedAction.Queue(new QueuedAction
			{
				WaitForTotalGameTime = responseDelay is null ? null : MG.inst.g.time + responseDelay.Value,
				Action = () =>
				{
					var storyKey = $".{Instance.SogginsDeck.GlobalName}_Double{(wasLaunchAction ? "Launch" : "")}Response";
					if (wasLaunchAction && DB.story.QuickLookup(g.state, storyKey) is null)
						storyKey = $".{Instance.SogginsDeck.GlobalName}_DoubleResponse";
					Narrative.SpeakBecauseOfAction(g, combat, storyKey);
				}
			});
			return true;
		}

		return false;
	}
}
