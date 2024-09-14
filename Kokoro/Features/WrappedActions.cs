using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public List<CardAction> GetWrappedCardActions(CardAction action)
			=> WrappedActionManager.Instance.GetWrappedCardActions(action).ToList();

		public List<CardAction> GetWrappedCardActionsRecursively(CardAction action)
			=> WrappedActionManager.Instance.GetWrappedCardActionsRecursively(action, includingWrapperActions: false).ToList();

		public List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions)
			=> WrappedActionManager.Instance.GetWrappedCardActionsRecursively(action, includingWrapperActions).ToList();

		public void RegisterWrappedActionHook(IWrappedActionHook hook, double priority)
			=> WrappedActionManager.Instance.Register(hook, priority);

		public void UnregisterWrappedActionHook(IWrappedActionHook hook)
			=> WrappedActionManager.Instance.Unregister(hook);
	}
}

internal sealed class WrappedActionManager : HookManager<IWrappedActionHook>
{
	internal static readonly WrappedActionManager Instance = new();
	
	public WrappedActionManager()
	{
		Register(ConditionalActionWrappedActionHook.Instance, 0);
		Register(ResourceCostActionWrappedActionHook.Instance, 0);
		Register(ContinuedActionWrappedActionHook.Instance, 0);
		Register(HiddenActionWrappedActionHook.Instance, 0);
		Register(SpoofedActionWrappedActionHook.Instance, 0);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetDataWithOverrides)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetDataWithOverrides_Transpiler))
		);
	}

	public IEnumerable<CardAction> GetWrappedCardActions(CardAction action)
	{
		foreach (var hook in Hooks)
		{
			var wrappedActions = hook.GetWrappedCardActions(action);
			if (wrappedActions is not null)
			{
				foreach (var wrappedAction in wrappedActions)
					yield return wrappedAction;
				yield break;
			}
		}
		yield return action;
	}

	public IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions)
	{
		foreach (var hook in Hooks)
		{
			var wrappedActions = hook.GetWrappedCardActions(action);
			if (wrappedActions is not null)
			{
				foreach (var wrappedAction in wrappedActions)
					foreach (var nestedWrappedAction in GetWrappedCardActionsRecursively(wrappedAction, includingWrapperActions))
						yield return nestedWrappedAction;
				if (includingWrapperActions)
					yield return action;
				yield break;
			}
		}
		yield return action;
	}
	
	private static IEnumerable<CodeInstruction> Card_GetActionsOverridden_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var wrappedActionsLocal = il.DeclareLocal(typeof(List<CardAction>));

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(1),
					ILMatches.Ldarg(2),
					ILMatches.Call("GetActions")
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Stloc, wrappedActionsLocal),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Transpiler_UnwrapActions)))
				])
				.Find([
					ILMatches.AnyLdloc.ExtractLabels(out var labels),
					ILMatches.Instruction(OpCodes.Ret)
				])
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Replace(new CodeInstruction(OpCodes.Ldloc, wrappedActionsLocal).WithLabels(labels))
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static List<CardAction> Card_GetActionsOverridden_Transpiler_UnwrapActions(List<CardAction> actions)
		=> actions.SelectMany(a => Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: false)).ToList();

	private static IEnumerable<CodeInstruction> Card_GetDataWithOverrides_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(1),
					ILMatches.Ldloc<Combat>(originalMethod),
					ILMatches.Call("GetActions")
				])
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetDataWithOverrides_Transpiler_UnwrapActions)))
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

	private static List<CardAction> Card_GetDataWithOverrides_Transpiler_UnwrapActions(List<CardAction> actions)
		=> actions.SelectMany(a => Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: false)).ToList();
}

public sealed class ConditionalActionWrappedActionHook : IWrappedActionHook
{
	public static ConditionalActionWrappedActionHook Instance { get; private set; } = new();

	private ConditionalActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AConditional conditional)
			return null;
		if (conditional.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
}

public sealed class ResourceCostActionWrappedActionHook : IWrappedActionHook
{
	public static ResourceCostActionWrappedActionHook Instance { get; private set; } = new();

	private ResourceCostActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AResourceCost resourceCostAction)
			return null;
		if (resourceCostAction.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
}

public sealed class ContinuedActionWrappedActionHook : IWrappedActionHook
{
	public static ContinuedActionWrappedActionHook Instance { get; private set; } = new();

	private ContinuedActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AContinued continued)
			return null;
		if (continued.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
}

public sealed class HiddenActionWrappedActionHook : IWrappedActionHook
{
	public static HiddenActionWrappedActionHook Instance { get; private set; } = new();

	private HiddenActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AHidden hidden)
			return null;
		if (hidden.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
}

public sealed class SpoofedActionWrappedActionHook : IWrappedActionHook
{
	public static SpoofedActionWrappedActionHook Instance { get; private set; } = new();

	private SpoofedActionWrappedActionHook() { }

	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not ASpoofed spoofed)
			return null;

		List<CardAction> results = [];
		if (spoofed.RenderAction is { } renderAction)
			results.Add(renderAction);
		if (spoofed.RealAction is { } realAction)
			results.Add(realAction);
		return results.Count == 0 ? null : results;
	}
}