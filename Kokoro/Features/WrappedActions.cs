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