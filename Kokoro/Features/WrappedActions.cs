using HarmonyLib;
using Microsoft.Extensions.Logging;
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
	
	partial class ActionApiImplementation
	{
		public List<CardAction> GetWrappedCardActions(CardAction action)
			=> WrappedActionManager.Instance.GetWrappedCardActions(action)?.ToList() ?? [action];

		public List<CardAction> GetWrappedCardActionsRecursively(CardAction action)
			=> WrappedActionManager.Instance.GetWrappedCardActionsRecursively(action, includingWrapperActions: false).ToList();

		public List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions)
			=> WrappedActionManager.Instance.GetWrappedCardActionsRecursively(action, includingWrapperActions).ToList();

		public void RegisterWrappedActionHook(IWrappedActionHook hook, double priority)
			=> WrappedActionManager.Instance.Register(hook, priority);

		public void UnregisterWrappedActionHook(IWrappedActionHook hook)
			=> WrappedActionManager.Instance.Unregister(hook);
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IWrappedActionsApi WrappedActions { get; } = new WrappedActionsApi();
		
		public sealed class WrappedActionsApi : IKokoroApi.IV2.IWrappedActionsApi
		{
			public void RegisterHook(IKokoroApi.IV2.IWrappedActionsApi.IHook hook, double priority = 0)
				=> WrappedActionManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IWrappedActionsApi.IHook hook)
				=> WrappedActionManager.Instance.Unregister(hook);

			public IEnumerable<CardAction>? GetWrappedCardActions(CardAction action)
				=> WrappedActionManager.Instance.GetWrappedCardActions(action);

			public IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action)
				=> WrappedActionManager.Instance.GetWrappedCardActionsRecursively(action, includingWrapperActions: false);

			public IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions)
				=> WrappedActionManager.Instance.GetWrappedCardActionsRecursively(action, includingWrapperActions);
			
			internal sealed class GetWrappedCardActionsArgs : IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly GetWrappedCardActionsArgs Instance = new();
				
				public CardAction Action { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class WrappedActionManager : VariedApiVersionHookManager<IKokoroApi.IV2.IWrappedActionsApi.IHook, IWrappedActionHook>
{
	internal static readonly WrappedActionManager Instance = new();

	private WrappedActionManager() : base(ModEntry.Instance.Package.Manifest.UniqueName, new HookMapper<IKokoroApi.IV2.IWrappedActionsApi.IHook, IWrappedActionHook>(hook => new V1ToV2WrappedActionHookWrapper(hook)))
	{
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

	public IEnumerable<CardAction>? GetWrappedCardActions(CardAction action)
	{
		var args = ApiImplementation.V2Api.WrappedActionsApi.GetWrappedCardActionsArgs.Instance;
		args.Action = action;
		return Hooks.Select(hook => hook.GetWrappedCardActions(args)).OfType<List<CardAction>>().FirstOrDefault();
	}

	public IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions)
	{
		var args = ApiImplementation.V2Api.WrappedActionsApi.GetWrappedCardActionsArgs.Instance;
		args.Action = action;
		
		foreach (var hook in Hooks)
		{
			var wrappedActions = hook.GetWrappedCardActions(args);
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

internal sealed class V1ToV2WrappedActionHookWrapper(IWrappedActionHook v1) : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
		=> v1.GetWrappedCardActions(args.Action);
}