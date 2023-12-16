using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static HarmonyLib.Code;

namespace Shockah.Kokoro;

internal static class CardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static int MakeAllActionIconsCounter = 0;
	private static int RenderActionCounter = 0;
	private static int LastRenderActionWidth = 0;
	private static readonly Stack<Matrix> CardRenderMatrixStack = new();

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(Card_GetAllTooltips_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(typeof(CardPatches), nameof(Card_RenderAction_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_Render_Scale_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_MakeAllActionIcons_Scale_Prefix)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_MakeAllActionIcons_Scale_Finalizer)), priority: Priority.Last)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_RenderAction_Scale_Prefix)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_RenderAction_Scale_Finalizer)), priority: Priority.Last)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__IconAndOrNumber") && m.ReturnType == typeof(void)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_RenderAction_IconAndOrNumber_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__ParenIconParen") && m.ReturnType == typeof(void)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_RenderAction_ParenIconParen_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_GetActionsOverridden_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetDataWithOverrides)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_GetDataWithOverrides_Transpiler))
		);
	}

	private static void ResetSpriteBatch()
	{
		try
		{
			Draw.EndAutoBatchFrame();
			Draw.StartAutoBatchFrame();
		}
		catch
		{
		}
	}

	private static IEnumerable<CodeInstruction> Card_Render_Scale_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldfld("description"),
					ILMatches.Brfalse
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.ExtractBranchTarget(out var branchTarget)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_Render_Scale_Transpiler_PushMatrix)))
				)
				.ForEach(
					SequenceMatcherRelativeBounds.After,
					new[]
					{
						ILMatches.LdcI4(51),
						ILMatches.Instruction(OpCodes.Conv_R8),
						ILMatches.Instruction(OpCodes.Call)
					},
					matcher =>
					{
						return matcher
							.PointerMatcher(SequenceMatcherRelativeElement.Last)
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_Render_Scale_Transpiler_ModifyAvailableWidth)))
							);
					},
					minExpectedOccurences: 2, maxExpectedOccurences: 2
				)
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_Render_Scale_Transpiler_PopMatrix))).WithLabels(labels)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Card_Render_Scale_Transpiler_PushMatrix(G g)
	{
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();
		CardRenderMatrixStack.Push(g.mg.cameraMatrix);
		Vector3 translation = new Vector3((float)box.rect.x + 2f, (float)box.rect.y + 31f, 0f) * g.mg.PIX_SCALE;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(-translation);
		MG.inst.cameraMatrix *= Matrix.CreateScale(1f, 1f, 1f);
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(translation);
		ResetSpriteBatch();
	}

	private static void Card_Render_Scale_Transpiler_PopMatrix()
	{
		MG.inst.cameraMatrix = CardRenderMatrixStack.Pop();
		ResetSpriteBatch();
	}

	private static double Card_Render_Scale_Transpiler_ModifyAvailableWidth(double width)
	{
		width *= 1f;
		return width;
	}

	private static void Card_MakeAllActionIcons_Scale_Prefix(G g, ref Matrix __state)
	{
		MakeAllActionIconsCounter++;
		if (MakeAllActionIconsCounter != 1)
			return;
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();

		__state = MG.inst.cameraMatrix;
		Vector3 translation = new Vector3((float)box.rect.x + 30f, (float)box.rect.y + 50f, 0f) * g.mg.PIX_SCALE;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(-translation);
		MG.inst.cameraMatrix *= Matrix.CreateScale(1f, 1f, 1f);
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(translation);
		ResetSpriteBatch();
	}

	private static void Card_MakeAllActionIcons_Scale_Finalizer(ref Matrix __state)
	{
		MakeAllActionIconsCounter--;
		if (MakeAllActionIconsCounter != 0)
			return;

		MG.inst.cameraMatrix = __state;
		ResetSpriteBatch();
	}

	private static void Card_RenderAction_Scale_Prefix(G g, bool dontDraw, ref Matrix __state)
	{
		if (dontDraw)
			return;
		RenderActionCounter++;
		if (RenderActionCounter != 1)
			return;
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();

		__state = MG.inst.cameraMatrix;
		Vector3 translation = new Vector3((float)box.rect.x + LastRenderActionWidth / 2f, (float)box.rect.y + 4f, 0f) * g.mg.PIX_SCALE;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(-translation);
		MG.inst.cameraMatrix *= Matrix.CreateScale(1f, 1f, 1f);
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(translation);
		ResetSpriteBatch();
	}

	private static void Card_RenderAction_Scale_Finalizer(bool dontDraw, int __result, ref Matrix __state)
	{
		LastRenderActionWidth = __result;
		if (dontDraw)
			return;
		RenderActionCounter--;
		if (RenderActionCounter != 0)
			return;

		MG.inst.cameraMatrix = __state;
		ResetSpriteBatch();
	}

	private static void Card_GetAllTooltips_Postfix(ref IEnumerable<Tooltip> __result)
	{
		__result = Instance.WormStatusManager.ModifyCardTooltips(__result);
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AConditional conditional)
			return true;
		if (conditional.Action is not { } wrappedAction)
			return false;

		bool oldActionDisabled = wrappedAction.disabled;
		bool faded = action.disabled || (conditional.FadeUnsatisfied && state.route is Combat combat && conditional.Expression?.GetValue(state, combat) == false);
		wrappedAction.disabled = faded;

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

		conditional.Expression?.Render(g, ref position, faded, dontDraw);
		if (conditional.Expression?.ShouldRenderQuestionMark(state, state.route as Combat) == true)
		{
			if (!dontDraw)
				Draw.Sprite((Spr)Instance.Content.QuestionMarkSprite.Id!.Value, position.x, position.y, color: faded ? Colors.disabledIconTint : Colors.white);
			position.x += SpriteLoader.Get((Spr)Instance.Content.QuestionMarkSprite.Id!.Value)?.Width ?? 0;
			position.x -= 1;
		}

		position.x += 2;
		if (wrappedAction is AAttack attack)
		{
			var shouldStun = state.EnumerateAllArtifacts().Any(a => a.ModifyAttacksToStun(state, state.route as Combat) == true);
			attack.stunEnemy = shouldStun;
		}

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, wrappedAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		wrappedAction.disabled = oldActionDisabled;

		return false;
	}

	private static IEnumerable<CodeInstruction> Card_RenderAction_IconAndOrNumber_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Call("GetValueOrDefault"),
					ILMatches.Ldfld("color")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg, 5),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[5].ParameterType.GetElementType(), "action")),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_RenderAction_IconAndOrNumber_Transpiler_ModifyXColor)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Color Card_RenderAction_IconAndOrNumber_Transpiler_ModifyXColor(Color currentColor, CardAction action)
	{
		if (!action.disabled)
			return currentColor;
		Color fadeColor = new(Colors.disabledText.r / Colors.textMain.r, Colors.disabledText.g / Colors.textMain.g, Colors.disabledText.b / Colors.textMain.b, Colors.disabledText.a / Colors.textMain.a);
		return currentColor * fadeColor;
	}

	private static IEnumerable<CodeInstruction> Card_RenderAction_ParenIconParen_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Call("GetValueOrDefault"),
					ILMatches.Ldfld("color")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "action")),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_RenderAction_ParenIconParen_Transpiler_ModifyXColor)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Color Card_RenderAction_ParenIconParen_Transpiler_ModifyXColor(Color currentColor, CardAction action)
	{
		if (!action.disabled)
			return currentColor;
		Color fadeColor = new(Colors.disabledText.r / Colors.textMain.r, Colors.disabledText.g / Colors.textMain.g, Colors.disabledText.b / Colors.textMain.b, Colors.disabledText.a / Colors.textMain.a);
		return currentColor * fadeColor;
	}

	private static IEnumerable<CodeInstruction> Card_GetActionsOverridden_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var wrappedActionsLocal = il.DeclareLocal(typeof(List<CardAction>));

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(1),
					ILMatches.Ldarg(2),
					ILMatches.Call("GetActions")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Stloc, wrappedActionsLocal),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_GetActionsOverridden_Transpiler_UnwrapActions)))
				)
				.Find(
					ILMatches.AnyLdloc,
					ILMatches.Instruction(OpCodes.Ret)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.ExtractLabels(out var labels)
				.Replace(new CodeInstruction(OpCodes.Ldloc, wrappedActionsLocal).WithLabels(labels))
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static List<CardAction> Card_GetActionsOverridden_Transpiler_UnwrapActions(List<CardAction> actions)
		=> actions.SelectMany(a => a.GetWrappedCardActionsRecursively()).ToList();

	private static IEnumerable<CodeInstruction> Card_GetDataWithOverrides_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(1),
					ILMatches.Ldloc<Combat>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Call("GetActions")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_GetDataWithOverrides_Transpiler_UnwrapActions)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static List<CardAction> Card_GetDataWithOverrides_Transpiler_UnwrapActions(List<CardAction> actions)
		=> actions.SelectMany(a => a.GetWrappedCardActionsRecursively()).ToList();
}
