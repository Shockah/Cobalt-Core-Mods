using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	public void RegisterCardRenderHook(ICardRenderHook hook, double priority)
		=> CardRenderManager.Instance.Register(hook, priority);

	public void UnregisterCardRenderHook(ICardRenderHook hook)
		=> CardRenderManager.Instance.Unregister(hook);

	public Font PinchCompactFont
		=> ModEntry.Instance.Content.PinchCompactFont;
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.ICardRenderingApi CardRendering { get; } = new CardRenderingApi();
		
		public sealed class CardRenderingApi : IKokoroApi.IV2.ICardRenderingApi
		{
			public void RegisterHook(IKokoroApi.IV2.ICardRenderingApi.IHook hook, double priority = 0)
				=> CardRenderManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.ICardRenderingApi.IHook hook)
				=> CardRenderManager.Instance.Unregister(hook);
			
			internal sealed class ShouldDisableCardRenderingTransformationsArgs : IKokoroApi.IV2.ICardRenderingApi.IHook.IShouldDisableCardRenderingTransformationsArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ShouldDisableCardRenderingTransformationsArgs Instance = new();
				
				public G G { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
			}
			
			internal sealed class ReplaceTextCardFontArgs : IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ReplaceTextCardFontArgs Instance = new();
				
				public G G { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
			}
			
			internal sealed class ModifyTextCardScaleArgs : IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyTextCardScaleArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ModifyTextCardScaleArgs Instance = new();
				
				public G G { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
			}
			
			internal sealed class ModifyNonTextCardRenderMatrixArgs : IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyNonTextCardRenderMatrixArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ModifyNonTextCardRenderMatrixArgs Instance = new();
				
				public G G { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public List<CardAction> Actions { get; internal set; } = null!;
			}
			
			internal sealed class ModifyCardActionRenderMatrixArgs : IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyCardActionRenderMatrixArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ModifyCardActionRenderMatrixArgs Instance = new();
				
				public G G { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public List<CardAction> Actions { get; internal set; } = null!;
				public CardAction Action { get; internal set; } = null!;
				public int ActionWidth { get; internal set; }
			}
		}
	}
}

internal sealed class CardRenderManager : VariedApiVersionHookManager<IKokoroApi.IV2.ICardRenderingApi.IHook, ICardRenderHook>
{
	internal static readonly CardRenderManager Instance = new();
	
	private static int MakeAllActionIconsCounter;
	private static int RenderActionCounter;
	private static int LastRenderActionWidth;
	private static Card? LastCard;
	private static List<CardAction>? LastCardActions;
	private static readonly Stack<Matrix?> CardRenderMatrixStack = new();

	private CardRenderManager() : base(ModEntry.Instance.Package.Manifest.UniqueName, new HookMapper<IKokoroApi.IV2.ICardRenderingApi.IHook, ICardRenderHook>(hook => new V1ToV2CardRenderingHookWrapper(hook)))
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Prefix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix_First)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Finalizer_Last)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Finalizer)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Transpiler))
		);
	}

	public bool ShouldDisableCardRenderingTransformations(G g, Card card)
	{
		var args = ApiImplementation.V2Api.CardRenderingApi.ShouldDisableCardRenderingTransformationsArgs.Instance;
		args.G = g;
		args.Card = card;
		return Hooks.Any(h => h.ShouldDisableCardRenderingTransformations(args));
	}

	public Font? ReplaceTextCardFont(G g, Card card)
	{
		var args = ApiImplementation.V2Api.CardRenderingApi.ReplaceTextCardFontArgs.Instance;
		args.G = g;
		args.Card = card;
		
		foreach (var hook in Hooks)
			if (hook.ReplaceTextCardFont(args) is { } font)
				return font;
		return null;
	}

	public Vec ModifyTextCardScale(G g, Card card)
	{
		var args = ApiImplementation.V2Api.CardRenderingApi.ModifyTextCardScaleArgs.Instance;
		args.G = g;
		args.Card = card;
		return Hooks.Aggregate(Vec.One, (v, hook) => v * hook.ModifyTextCardScale(args));
	}

	public Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions)
	{
		var args = ApiImplementation.V2Api.CardRenderingApi.ModifyNonTextCardRenderMatrixArgs.Instance;
		args.G = g;
		args.Card = card;
		args.Actions = actions;
		return Hooks.Aggregate(Matrix.Identity, (m, hook) => m * hook.ModifyNonTextCardRenderMatrix(args));
	}

	public Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth)
	{
		var args = ApiImplementation.V2Api.CardRenderingApi.ModifyCardActionRenderMatrixArgs.Instance;
		args.G = g;
		args.Card = card;
		args.Actions = actions;
		args.Action = action;
		args.ActionWidth = actionWidth;
		return Hooks.Aggregate(Matrix.Identity, (m, hook) => m * hook.ModifyCardActionRenderMatrix(args));
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
			// ignored
		}
	}

	private static void Card_Render_Prefix(Card __instance)
	{
		MakeAllActionIconsCounter = 0;
		RenderActionCounter = 0;
		LastRenderActionWidth = 0;
		LastCard = __instance;
		LastCardActions = null;
		CardRenderMatrixStack.Clear();
	}
	
	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var declaringType = MethodBase.GetCurrentMethod()!.DeclaringType!;
			var modifiedScaleLocal = il.DeclareLocal(typeof(Vec));

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld("description"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				])
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(declaringType, nameof(Card_Render_Transpiler_PushMatrix))),
					new CodeInstruction(OpCodes.Stloc, modifiedScaleLocal)
				])
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld("description")
				])
				.Find(ILMatches.Instruction(OpCodes.Ldnull))
				.Replace([
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(declaringType, nameof(Card_Render_Transpiler_ReplaceCardTextFont)))
				])
				.ForEach(
					SequenceMatcherRelativeBounds.After,
					[
						ILMatches.LdcI4(51),
						ILMatches.Instruction(OpCodes.Conv_R8),
						ILMatches.Instruction(OpCodes.Call)
					],
					matcher => matcher
						.PointerMatcher(SequenceMatcherRelativeElement.Last)
						.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
							new CodeInstruction(OpCodes.Ldloc, modifiedScaleLocal),
							new CodeInstruction(OpCodes.Ldarg_0),
							new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(declaringType, nameof(Card_Render_Transpiler_ModifyAvailableWidth)))
						]),
					minExpectedOccurences: 2, maxExpectedOccurences: 2
				)
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(declaringType, nameof(Card_Render_Transpiler_PopMatrix))).WithLabels(labels)
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
	
	private static Font? Card_Render_Transpiler_ReplaceCardTextFont(Card card, G g)
		=> Instance.ReplaceTextCardFont(g, card);

	private static Vec Card_Render_Transpiler_PushMatrix(Card card, G g)
	{
		if (Instance.ShouldDisableCardRenderingTransformations(g, card))
		{
			CardRenderMatrixStack.Push(null);
			return Vec.One;
		}
		var modifiedScale = Instance.ModifyTextCardScale(g, card);
		if (modifiedScale is { x: 1, y: 1 })
		{
			CardRenderMatrixStack.Push(null);
			return Vec.One;
		}

		CardRenderMatrixStack.Push(g.mg.cameraMatrix);
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();
		var translation = new Vector3((float)box.rect.x + 2f, (float)box.rect.y + 31f, 0f) * g.mg.PIX_SCALE;
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
		if (Instance.ShouldDisableCardRenderingTransformations(g, LastCard))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}
		var modifiedMatrix = LastCardActions is null ? Matrix.Identity : Instance.ModifyCardActionRenderMatrix(g, LastCard, LastCardActions, action, LastRenderActionWidth);
		if (modifiedMatrix.Equals(Matrix.Identity))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}

		CardRenderMatrixStack.Push(g.mg.cameraMatrix);
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();
		var translation = new Vector3((float)box.rect.x + LastRenderActionWidth / 2f, (float)box.rect.y + 4f, 0f) * g.mg.PIX_SCALE;
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
	
	private static IEnumerable<CodeInstruction> Card_MakeAllActionIcons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var actionsOverriddenLocal = il.DeclareLocal(typeof(List<CardAction>));

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("GetActionsOverridden"))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Stloc, actionsOverriddenLocal)
				])
				.Find([
					ILMatches.Call("CharacterIsMissing"),
					ILMatches.Brfalse,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloc, actionsOverriddenLocal),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Transpiler_PushMatrix)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
	
	private static void Card_MakeAllActionIcons_Transpiler_PushMatrix(Card card, G g, List<CardAction> actions)
	{
		MakeAllActionIconsCounter++;
		if (MakeAllActionIconsCounter != 1)
			return;

		LastCardActions = actions;

		if (Instance.ShouldDisableCardRenderingTransformations(g, card))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}
		var modifiedMatrix = Instance.ModifyNonTextCardRenderMatrix(g, card, actions);
		if (modifiedMatrix.Equals(Matrix.Identity))
		{
			CardRenderMatrixStack.Push(null);
			return;
		}

		CardRenderMatrixStack.Push(g.mg.cameraMatrix);
		var box = g.uiStack.TryPeek(out var existingRect) ? existingRect : new();
		var translation = new Vector3((float)box.rect.x + 30f, (float)box.rect.y + 50f, 0f) * g.mg.PIX_SCALE;
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
}

internal sealed class V1ToV2CardRenderingHookWrapper(ICardRenderHook v1) : IKokoroApi.IV2.ICardRenderingApi.IHook
{
	public bool ShouldDisableCardRenderingTransformations(IKokoroApi.IV2.ICardRenderingApi.IHook.IShouldDisableCardRenderingTransformationsArgs args)
		=> v1.ShouldDisableCardRenderingTransformations(args.G, args.Card);
		
	public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
		=> v1.ReplaceTextCardFont(args.G, args.Card);
		
	public Vec ModifyTextCardScale(IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyTextCardScaleArgs args)
		=> v1.ModifyTextCardScale(args.G, args.Card);
		
	public Matrix ModifyNonTextCardRenderMatrix(IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyNonTextCardRenderMatrixArgs args)
		=> v1.ModifyNonTextCardRenderMatrix(args.G, args.Card, args.Actions);

	public Matrix ModifyCardActionRenderMatrix(IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyCardActionRenderMatrixArgs args)
		=> v1.ModifyCardActionRenderMatrix(args.G, args.Card, args.Actions, args.Action, args.ActionWidth);
}