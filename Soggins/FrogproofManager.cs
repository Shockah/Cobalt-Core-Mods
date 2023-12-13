using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Soggins;

public sealed class FrogproofManager : HookManager<IFrogproofHook>
{
	private static ModEntry Instance => ModEntry.Instance;

	internal FrogproofManager() : base()
	{
		Register(FrogproofCardTraitFrogproofHook.Instance, 0);
		Register(TrashCardFrogproofHook.Instance, 1);
		Register(FrogproofingFrogproofHook.Instance, -10);
	}

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(typeof(FrogproofManager), nameof(Card_Render_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(typeof(FrogproofManager), nameof(Card_GetAllTooltips_Postfix))
		);
	}

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> GetHandlingHook(state, combat, card, context) is not null;

	public IFrogproofHook? GetHandlingHook(State state, Combat? combat, Card card, FrogproofHookContext context = FrogproofHookContext.Action)
	{
		foreach (var hook in Hooks)
		{
			var hookResult = hook.GetFrogproofType(state, combat, card, context);
			if (hookResult == FrogproofType.None)
				return null;
			else if (hookResult != null)
				return hook;
		}
		return null;
	}

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldfld("buoyant"),
					ILMatches.Brfalse
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.ExtractLabels(out var labels)
				.AnchorPointer(out Guid findAnchor)
				.Find(
					ILMatches.Ldloc<Vec>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldfld("y"),
					ILMatches.LdcI4(8),
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Instruction(OpCodes.Dup),
					ILMatches.LdcI4(1),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocVec)
				.Advance(3)
				.CreateLdlocaInstruction(out var ldlocaCardTraitIndex)
				.PointerMatcher(findAnchor)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldarg_0),
					ldlocaCardTraitIndex,
					ldlocVec,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(FrogproofManager), nameof(Card_Render_Transpiler_RenderFrogproofIfNeeded)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Card_Render_Transpiler_RenderFrogproofIfNeeded(G g, State? state, Card card, ref int cardTraitIndex, Vec vec)
	{
		state ??= g.state;
		if (!Instance.FrogproofManager.IsFrogproof(state, state.route as Combat, card, FrogproofHookContext.Rendering))
			return;
		Draw.Sprite((Spr)Instance.FrogproofSprite.Id!.Value, vec.x, vec.y - 8 * cardTraitIndex++);
	}

	private static void Card_GetAllTooltips_Postfix(Card __instance, G g, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;
		if (!Instance.FrogproofManager.IsFrogproof(s, s.route as Combat, __instance, FrogproofHookContext.Rendering))
			return;

		static IEnumerable<Tooltip> ModifyTooltips(IEnumerable<Tooltip> tooltips)
		{
			bool yieldedFrogproof = false;

			foreach (var tooltip in tooltips)
			{
				if (!yieldedFrogproof && tooltip is TTGlossary glossary && glossary.key.StartsWith("cardtrait.") && glossary.key != "cardtrait.unplayable")
				{
					yield return Instance.Api.FrogproofCardTraitTooltip;
					yieldedFrogproof = true;
				}
				yield return tooltip;
			}

			if (!yieldedFrogproof)
				yield return Instance.Api.FrogproofCardTraitTooltip;
		}

		__result = ModifyTooltips(__result);
	}
}

public sealed class FrogproofCardTraitFrogproofHook : IFrogproofHook
{
	public static FrogproofCardTraitFrogproofHook Instance { get; private set; } = new();

	private FrogproofCardTraitFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> card is IFrogproofCard frogproofCard && frogproofCard.IsFrogproof(state, combat) ? FrogproofType.Innate : null;

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}

public sealed class TrashCardFrogproofHook : IFrogproofHook
{
	public static TrashCardFrogproofHook Instance { get; private set; } = new();

	private TrashCardFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> card.GetMeta().deck == Deck.trash ? FrogproofType.Innate : null;

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}

public sealed class FrogproofingFrogproofHook : IFrogproofHook
{
	public static FrogproofingFrogproofHook Instance { get; private set; } = new();

	private FrogproofingFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> context == FrogproofHookContext.Action && state.ship.Get((Status)ModEntry.Instance.FrogproofingStatus.Id!.Value) > 0 ? FrogproofType.Paid : null;

	public void PayForFrogproof(State state, Combat? combat, Card card)
		=> state.ship.Add((Status)ModEntry.Instance.FrogproofingStatus.Id!.Value, -1);
}