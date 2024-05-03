using daisyowl.text;
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

namespace Shockah.Kokoro;

internal static class CardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static int MakeAllActionIconsCounter = 0;
	private static int RenderActionCounter = 0;
	private static int LastRenderActionWidth = 0;
	private static Card? LastCard = null;
	private static List<CardAction>? LastCardActions = null;
	private static Dictionary<string, int>? CurrentResourceState = null;
	private static Dictionary<string, int>? CurrentNonDrawingResourceState = null;
	private static readonly Stack<Matrix?> CardRenderMatrixStack = new();

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(Card_GetAllTooltips_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(typeof(CardPatches), nameof(Card_Render_Prefix)),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(Card_Render_Postfix)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_Render_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			prefix: new HarmonyMethod(typeof(CardPatches), nameof(Card_MakeAllActionIcons_Prefix)),
			finalizer: new HarmonyMethod(typeof(CardPatches), nameof(Card_MakeAllActionIcons_Finalizer)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_MakeAllActionIcons_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(typeof(CardPatches), nameof(Card_RenderAction_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_RenderAction_Prefix_First)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_RenderAction_Finalizer_Last)), priority: Priority.Last)
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
			original: () => typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__VarAssignment") && m.ReturnType == typeof(void)),
			transpiler: new HarmonyMethod(typeof(CardPatches), nameof(Card_RenderAction_VarAssignment_Transpiler))
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
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(State), nameof(State.Render)),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(State_Render_Postfix))
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

	private static void Card_GetAllTooltips_Postfix(ref IEnumerable<Tooltip> __result)
	{
		__result = Instance.WormStatusManager.ModifyCardTooltips(__result);
	}

	private static void Card_Render_Prefix(Card __instance)
	{
		MakeAllActionIconsCounter = 0;
		RenderActionCounter = 0;
		LastRenderActionWidth = 0;
		LastCard = __instance;
		LastCardActions = null;
		CurrentResourceState = null;
		CurrentNonDrawingResourceState = null;
		CardRenderMatrixStack.Clear();
	}

	private static void Card_Render_Postfix(Card __instance, G g, Vec? posOverride, State? fakeState, double? overrideWidth)
	{
		var state = fakeState ?? g.state;
		if (state.route is not Combat combat)
			return;
		if (!Instance.RedrawStatusManager.IsRedrawPossible(state, combat, __instance))
			return;

		var position = posOverride ?? __instance.pos;
		position += new Vec(0.0, __instance.hoverAnim * -2.0 + Mutil.Parabola(__instance.flipAnim) * -10.0 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * (double)Math.Sign(__instance.flopAnim));
		position += new Vec(((overrideWidth ?? 59) - 21) / 2.0, 82 - 13 / 2.0 - 0.5);
		position = position.round();

		var result = SharedArt.ButtonSprite(
			g,
			new Rect(position.x, position.y, 19, 13),
			new UIKey((UK)21370099, __instance.uuid),
			(Spr)ModEntry.Instance.Content.RedrawButtonSprite.Id!.Value,
			(Spr)ModEntry.Instance.Content.RedrawButtonOnSprite.Id!.Value,
			onMouseDown: new MouseDownHandler(() =>
			{
				if (Instance.RedrawStatusManager.GetHandlingHook(state, combat, __instance) is not { } hook)
					return;

				hook.PayForRedraw(state, combat, __instance);
				hook.DoRedraw(state, combat, __instance);
				Instance.RedrawStatusManager.AfterRedraw(state, combat, __instance, hook);
			})
		);
		if (result.isHover)
			g.tooltips.Add(position + new Vec(30, 10), Instance.Api.GetRedrawStatusTooltip());
	}

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var modifiedScaleLocal = il.DeclareLocal(typeof(Vec));

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld("description"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_Render_Transpiler_PushMatrix))),
					new CodeInstruction(OpCodes.Stloc, modifiedScaleLocal)
				)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld("description")
				)
				.Find(ILMatches.Instruction(OpCodes.Ldnull))
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_Render_Transpiler_ReplaceCardTextFont)))
				)
				.ForEach(
					SequenceMatcherRelativeBounds.After,
					[
						ILMatches.LdcI4(51),
						ILMatches.Instruction(OpCodes.Conv_R8),
						ILMatches.Instruction(OpCodes.Call)
					],
					matcher =>
					{
						return matcher
							.PointerMatcher(SequenceMatcherRelativeElement.Last)
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Ldloc, modifiedScaleLocal),
								new CodeInstruction(OpCodes.Ldarg_0),
								new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_Render_Transpiler_ModifyAvailableWidth)))
							);
					},
					minExpectedOccurences: 2, maxExpectedOccurences: 2
				)
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_Render_Transpiler_PopMatrix))).WithLabels(labels)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Font? Card_Render_Transpiler_ReplaceCardTextFont(Card card, G g)
		=> Instance.CardRenderManager.ReplaceTextCardFont(g, card);

	private static Vec Card_Render_Transpiler_PushMatrix(Card card, G g)
	{
		if (Instance.CardRenderManager.ShouldDisableCardRenderingTransformations(g, card))
		{
			CardRenderMatrixStack.Push(null);
			return Vec.One;
		}
		var modifiedScale = Instance.CardRenderManager.ModifyTextCardScale(g, card);
		if (modifiedScale.x == 1 && modifiedScale.y == 1)
		{
			CardRenderMatrixStack.Push(null);
			return Vec.One;
		}

		CardRenderMatrixStack.Push(g.mg.cameraMatrix);
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();
		Vector3 translation = new Vector3((float)box.rect.x + 2f, (float)box.rect.y + 31f, 0f) * g.mg.PIX_SCALE;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(-translation);
		MG.inst.cameraMatrix *= Matrix.CreateScale((float)modifiedScale.x, (float)modifiedScale.y, 1f);
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(translation);
		ResetSpriteBatch();
		return modifiedScale;
	}

	private static void Card_Render_Transpiler_PopMatrix()
	{
		var matrix = CardRenderMatrixStack.Pop();
		if (matrix is null)
			return;
		MG.inst.cameraMatrix = matrix.Value;
		ResetSpriteBatch();
	}

	private static double Card_Render_Transpiler_ModifyAvailableWidth(double width, Vec modifiedScale, Card card)
		=> (width + (card.GetType().Assembly == typeof(G).Assembly ? 0 : 1)) / modifiedScale.x;

	private static IEnumerable<CodeInstruction> Card_MakeAllActionIcons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var actionsOverriddenLocal = il.DeclareLocal(typeof(List<CardAction>));

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("GetActionsOverridden"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_MakeAllActionIcons_Transpiler_ModifyActions))),
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Stloc, actionsOverriddenLocal)
				)
				.Find(
					ILMatches.Call("CharacterIsMissing"),
					ILMatches.Brfalse,
					ILMatches.Instruction(OpCodes.Ret)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloc, actionsOverriddenLocal),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_MakeAllActionIcons_Transpiler_PushMatrix)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static List<CardAction> Card_MakeAllActionIcons_Transpiler_ModifyActions(List<CardAction> actions, Card card, State state)
	{
		var resources = actions
			.SelectMany(a => Instance.WrappedActionManager.GetWrappedCardActionsRecursively(a, includingWrapperActions: true))
			.OfType<AResourceCost>()
			.SelectMany(a => a.Costs ?? new())
			.SelectMany(c => c.PotentialResources)
			.ToList();

		CurrentResourceState = resources.Count == 0 ? new() : AResourceCost.GetCurrentResourceState(state, state.route as Combat ?? DB.fakeCombat, resources);
		if (CurrentResourceState.ContainsKey("Energy"))
			CurrentResourceState["Energy"] -= card.GetDataWithOverrides(state).cost;
		CurrentNonDrawingResourceState = new(CurrentResourceState);

		return actions.Where(a => a is not AHidden).ToList();
	}

	private static void Card_MakeAllActionIcons_Transpiler_PushMatrix(Card card, G g, List<CardAction> actions)
	{
		MakeAllActionIconsCounter++;
		if (MakeAllActionIconsCounter != 1)
			return;

		LastCardActions = actions;

		if (Instance.CardRenderManager.ShouldDisableCardRenderingTransformations(g, card))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}
		var modifiedMatrix = Instance.CardRenderManager.ModifyNonTextCardRenderMatrix(g, card, actions);
		if (modifiedMatrix.Equals(Matrix.Identity))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}

		CardRenderMatrixStack.Push(g.mg.cameraMatrix);
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();
		Vector3 translation = new Vector3((float)box.rect.x + 30f, (float)box.rect.y + 50f, 0f) * g.mg.PIX_SCALE;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(-translation);
		MG.inst.cameraMatrix *= modifiedMatrix;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(translation);
		ResetSpriteBatch();
	}

	private static void Card_MakeAllActionIcons_Prefix(Card __instance)
	{
		LastCard = __instance;
	}

	private static void Card_MakeAllActionIcons_Finalizer()
	{
		LastCard = null;
		CurrentResourceState = null;
		CurrentNonDrawingResourceState = null;

		MakeAllActionIconsCounter--;
		if (MakeAllActionIconsCounter != 0)
			return;
		LastCardActions = null;

		var matrix = CardRenderMatrixStack.Pop();
		if (matrix is null)
			return;
		MG.inst.cameraMatrix = matrix.Value;
		ResetSpriteBatch();
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is AConditional conditional)
		{
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
				if (shouldStun)
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
		else if (action is AResourceCost resourceCostAction)
		{
			if (resourceCostAction.Action is not { } wrappedAction)
				return false;
			var resourceState = (dontDraw ? CurrentNonDrawingResourceState : CurrentResourceState) ?? new();

			bool oldActionDisabled = wrappedAction.disabled;
			wrappedAction.disabled = action.disabled;

			var position = g.Push(rect: new()).rect.xy;
			int initialX = (int)position.x;

			var (payment, groupedPayment, _) = AResourceCost.GetResourcePayment(resourceState, resourceCostAction.Costs ?? new());
			resourceCostAction.RenderCosts(g, ref position, action.disabled, dontDraw, payment);
			if (!action.disabled)
				foreach (var (resourceKey, resourceAmount) in groupedPayment)
					resourceState[resourceKey] = resourceState.GetValueOrDefault(resourceKey) - resourceAmount;

			position.x += 2;
			if (wrappedAction is AAttack attack)
			{
				var shouldStun = state.EnumerateAllArtifacts().Any(a => a.ModifyAttacksToStun(state, state.route as Combat) == true);
				if (shouldStun)
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
		else if (action is AContinued continuedAction)
		{
			if (continuedAction.Action is not { } wrappedAction)
				return false;

			bool oldActionDisabled = wrappedAction.disabled;
			wrappedAction.disabled = action.disabled;

			var position = g.Push(rect: new()).rect.xy;
			int initialX = (int)position.x;
			if (wrappedAction is AAttack attack)
			{
				var shouldStun = state.EnumerateAllArtifacts().Any(a => a.ModifyAttacksToStun(state, state.route as Combat) == true);
				if (shouldStun)
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
		else if (action is ASpoofed spoofedAction)
		{
			if ((spoofedAction.RenderAction ?? spoofedAction.RealAction) is not { } actionToRender)
				return true;

			__result = Card.RenderAction(g, state, actionToRender, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			return false;
		}

		return true;
	}

	private static void Card_RenderAction_Prefix_First(G g, CardAction action, bool dontDraw)
	{
		if (dontDraw)
			return;
		RenderActionCounter++;
		if (RenderActionCounter != 1)
			return;

		if (LastCard is null)
		{
			CardRenderMatrixStack.Push(null);
			return;
		}
		if (Instance.CardRenderManager.ShouldDisableCardRenderingTransformations(g, LastCard))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}
		var modifiedMatrix = LastCardActions is null ? Matrix.Identity : Instance.CardRenderManager.ModifyCardActionRenderMatrix(g, LastCard, LastCardActions, action, LastRenderActionWidth);
		if (modifiedMatrix.Equals(Matrix.Identity))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}

		CardRenderMatrixStack.Push(g.mg.cameraMatrix);
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();
		Vector3 translation = new Vector3((float)box.rect.x + LastRenderActionWidth / 2f, (float)box.rect.y + 4f, 0f) * g.mg.PIX_SCALE;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(-translation);
		MG.inst.cameraMatrix *= modifiedMatrix;
		MG.inst.cameraMatrix *= Matrix.CreateTranslation(translation);
		ResetSpriteBatch();
	}

	private static void Card_RenderAction_Finalizer_Last(bool dontDraw, int __result)
	{
		LastRenderActionWidth = __result;
		if (dontDraw)
			return;
		RenderActionCounter--;
		if (RenderActionCounter != 0)
			return;

		var matrix = CardRenderMatrixStack.Pop();
		if (matrix is null)
			return;
		MG.inst.cameraMatrix = matrix.Value;
		ResetSpriteBatch();
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

	private static IEnumerable<CodeInstruction> Card_RenderAction_VarAssignment_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Instruction(OpCodes.Ret))
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "g")),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "action")),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "dontDraw")),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "w")),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_RenderAction_VarAssignment_Transpiler_Outgoing))),
					new CodeInstruction(OpCodes.Stfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "w"))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static int Card_RenderAction_VarAssignment_Transpiler_Outgoing(G g, CardAction action, bool dontDraw, int w)
	{
		if (action is not AVariableHint variableHint)
			return w;
		if (variableHint.hand)
			return w;
		if (Instance.Api.ObtainExtensionData(variableHint, "targetPlayer", () => true))
			return w;

		if (!dontDraw)
		{
			Vec v = g.Push(null, new Rect(w)).rect.xy;
			Color spriteColor = variableHint.disabled ? Colors.disabledIconTint : new Color("ffffff");
			Draw.Sprite(StableSpr.icons_outgoing, v.x, v.y, color: spriteColor);
			g.Pop();
		}
		w += 8;
		return w;
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
					ILMatches.AnyLdloc.ExtractLabels(out var labels),
					ILMatches.Instruction(OpCodes.Ret)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
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
		=> actions.SelectMany(a => Instance.WrappedActionManager.GetWrappedCardActionsRecursively(a, includingWrapperActions: false)).ToList();

	private static IEnumerable<CodeInstruction> Card_GetDataWithOverrides_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(1),
					ILMatches.Ldloc<Combat>(originalMethod),
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
		=> actions.SelectMany(a => Instance.WrappedActionManager.GetWrappedCardActionsRecursively(a, includingWrapperActions: false)).ToList();

	private static void State_Render_Postfix()
	{
		MakeAllActionIconsCounter = 0;
		RenderActionCounter = 0;
		LastRenderActionWidth = 0;
		LastCard = null;
		LastCardActions = null;
		CurrentResourceState = null;
		CurrentNonDrawingResourceState = null;
		CardRenderMatrixStack.Clear();
	}
}
