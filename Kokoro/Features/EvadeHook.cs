using FSPRO;
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
	#region V1

	public IEvadeHook VanillaEvadeHook
	{
		get
		{
			if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(VanillaEvadeHook))))
				ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(VanillaEvadeHook));
			return FakeVanillaEvadeV1Hook.Instance;
		}
	}

	public IEvadeHook VanillaDebugEvadeHook
	{
		get
		{
			if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(VanillaDebugEvadeHook))))
				ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(VanillaDebugEvadeHook));
			return FakeVanillaDebugEvadeV1Hook.Instance;
		}
	}

	public void RegisterEvadeHook(IEvadeHook hook, double priority)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(RegisterEvadeHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(RegisterEvadeHook));
	}

	public void UnregisterEvadeHook(IEvadeHook hook)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(UnregisterEvadeHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(UnregisterEvadeHook));
	}

	public bool IsEvadePossible(State state, Combat combat, int direction, EvadeHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(IsEvadePossible))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(IsEvadePossible));
		return false;
	}

	public bool IsEvadePossible(State state, Combat combat, EvadeHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(IsEvadePossible))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(IsEvadePossible));
		return false;
	}

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, int direction, EvadeHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(GetEvadeHandlingHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(GetEvadeHandlingHook));
		return null;
	}

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, EvadeHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(GetEvadeHandlingHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(GetEvadeHandlingHook));
		return null;
	}

	public void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(AfterEvade))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(AfterEvade));
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IEvadeHookApi EvadeHook { get; } = new EvadeHookApi();
		
		public sealed class EvadeHookApi : IKokoroApi.IV2.IEvadeHookApi
		{
			public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry DefaultAction
				=> EvadeManager.Instance.DefaultActionEntry;
			
			public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption DefaultActionPaymentOption
				=> DefaultEvadePaymentOption.Instance;
			
			public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption DebugActionPaymentOption
				=> DebugEvadePaymentOption.Instance;
			
			public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition DefaultActionLockdownPrecondition
				=> LockdownEvadePrecondition.Instance;
			
			public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition DefaultActionAnchorPrecondition
				=> AnchorEvadePrecondition.Instance;

			public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry RegisterAction(IKokoroApi.IV2.IEvadeHookApi.IEvadeAction action, double priority = 0)
			{
				var entry = new EvadeActionEntry(action);
				EvadeManager.Instance.ActionEntries.Add(entry, priority);
				return entry;
			}

			public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult MakePreconditionResult(bool isAllowed)
				=> new EvadePreconditionResult(isAllowed);

			public void RegisterHook(IKokoroApi.IV2.IEvadeHookApi.IHook hook, double priority = 0)
				=> EvadeManager.Instance.HookManager.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IEvadeHookApi.IHook hook)
				=> EvadeManager.Instance.HookManager.Unregister(hook);
			
			internal sealed class EvadeActionEntry(
				IKokoroApi.IV2.IEvadeHookApi.IEvadeAction action
			) : IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry
			{
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeAction Action { get; } = action;

				public IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption> PaymentOptions
					=> PaymentOptionsOrderedList;

				public IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition> Preconditions
					=> PreconditionsOrderedList;

				private readonly OrderedList<IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption, double> PaymentOptionsOrderedList = new(ascending: false);
				private readonly OrderedList<IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition, double> PreconditionsOrderedList = new(ascending: false);

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry RegisterPaymentOption(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption paymentOption, double priority = 0)
				{
					PaymentOptionsOrderedList.Add(paymentOption, priority);
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry UnregisterPaymentOption(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption paymentOption)
				{
					PaymentOptionsOrderedList.Remove(paymentOption);
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry RegisterPrecondition(IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition precondition, double priority = 0)
				{
					PreconditionsOrderedList.Add(precondition, priority);
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry UnregisterPrecondition(IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition precondition)
				{
					PreconditionsOrderedList.Remove(precondition);
					return this;
				}
			}
			
			internal sealed class EvadePreconditionResult(
				bool isAllowed
			) : IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult
			{
				public bool IsAllowed { get; set; } = isAllowed;
				public bool ShakeShipOnFail { get; set; } = true;
				public IList<CardAction> ActionsOnFail { get; set; } = [];
				
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult SetIsAllowed(bool value)
				{
					IsAllowed = value;
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult SetShakeShipOnFail(bool value)
				{
					ShakeShipOnFail = value;
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult SetActionsOnFail(IList<CardAction> value)
				{
					ActionsOnFail = value;
					return this;
				}
			}
			
			internal sealed class ActionCanDoEvadeArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadeAction.ICanDoEvadeArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ActionCanDoEvadeArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
			}
			
			internal sealed class ActionProvideEvadeActionsArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadeAction.IProvideEvadeActionsArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ActionProvideEvadeActionsArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
			}
			
			internal sealed class PaymentOptionCanPayForEvadeArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.ICanPayForEvadeArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PaymentOptionCanPayForEvadeArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PaymentOptionProvideEvadePaymentActionsArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IProvideEvadePaymentActionsArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PaymentOptionProvideEvadePaymentActionsArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PreconditionIsEvadeAllowedArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IIsEvadeAllowedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PreconditionIsEvadeAllowedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class HookIsEvadeActionEnabledArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadeActionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsEvadeActionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class HookIsEvadePaymentOptionEnabledArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePaymentOptionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsEvadePaymentOptionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
			}
			
			internal sealed class HookIsEvadePreconditionEnabledArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePreconditionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsEvadePreconditionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition Precondition { get; internal set; } = null!;
			}
			
			internal sealed class HookAfterEvadeArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IAfterEvadeArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookAfterEvadeArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class EvadeManager
{
	internal static readonly EvadeManager Instance = new();

	internal readonly HookManager<IKokoroApi.IV2.IEvadeHookApi.IHook> HookManager;
	internal readonly OrderedList<IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry, double> ActionEntries;
	internal readonly IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry DefaultActionEntry;
	
	private EvadeManager()
	{
		HookManager = new(ModEntry.Instance.Package.Manifest.UniqueName);
		ActionEntries = new(ascending: false);
		
		var defaultActionEntry = new ApiImplementation.V2Api.EvadeHookApi.EvadeActionEntry(DefaultEvadeAction.Instance)
			.RegisterPaymentOption(DefaultEvadePaymentOption.Instance)
			.RegisterPaymentOption(DebugEvadePaymentOption.Instance, 1_000_000_000)
			.RegisterPrecondition(LockdownEvadePrecondition.Instance)
			.RegisterPrecondition(AnchorEvadePrecondition.Instance);
		DefaultActionEntry = defaultActionEntry;
		ActionEntries.Add(defaultActionEntry, 0);
		
		HookManager.Register(DebugEvadeHook.Instance, 1_000_000_000);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderMoveButtons)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DoEvade)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DoEvade_Prefix))
		);
	}

	private static IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry> FilterEnabled(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction, IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry> enumerable)
	{
		var args = ApiImplementation.V2Api.EvadeHookApi.HookIsEvadeActionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(e =>
		{
			args.Entry = e;
			return hooks.All(h => h.IsEvadeActionEnabled(args));
		});
	}

	private static IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption> FilterEnabled(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction, IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry entry, IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption> enumerable)
	{
		var args = ApiImplementation.V2Api.EvadeHookApi.HookIsEvadePaymentOptionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		args.Entry = entry;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(paymentOption =>
		{
			args.PaymentOption = paymentOption;
			return hooks.All(h => h.IsEvadePaymentOptionEnabled(args));
		});
	}

	private static IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition> FilterEnabled(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction, IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry entry, IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition> enumerable)
	{
		var args = ApiImplementation.V2Api.EvadeHookApi.HookIsEvadePreconditionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		args.Entry = entry;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(precondition =>
		{
			args.Precondition = precondition;
			return hooks.All(h => h.IsEvadePreconditionEnabled(args));
		});
	}
	
	private static IEnumerable<CodeInstruction> Combat_RenderMoveButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var leftEndLabel = il.DefineLabel();
			var rightEndLabel = il.DefineLabel();

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(1).ExtractLabels(out var labels),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.evade),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.Replace(new CodeInstruction(OpCodes.Nop).WithLabels(labels))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldarg(1).Anchor(out var gPointer1),
					ILMatches.LdcI4((int)StableUK.btn_move_left),
					ILMatches.AnyCall,
					ILMatches.Stloc<UIKey>(originalMethod)
				])
				.Anchors()
				.PointerMatcher(gPointer1)
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldc_I4, -1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brfalse, leftEndLabel),
					new CodeInstruction(OpCodes.Ldarg_1)
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Ldarg(1),
					ILMatches.LdcI4((int)StableUK.btn_move_right),
					ILMatches.AnyCall,
					ILMatches.Stloc<UIKey>(originalMethod)
				])
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Encompass(SequenceMatcherEncompassDirection.Before, 3)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(leftEndLabel),
					new CodeInstruction(OpCodes.Ldc_I4, 1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brfalse, rightEndLabel)
				])
				.PointerMatcher(SequenceMatcherRelativeElement.LastInWholeSequence)
				.AddLabel(rightEndLabel)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool Combat_RenderMoveButtons_Transpiler_ShouldRender(G g, int direction)
	{
		if (g.state.route is not Combat combat)
			return false;

		return GetActionEntry() is not null;

		IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry? GetActionEntry()
		{
			var typedDirection = (IKokoroApi.IV2.IEvadeHookApi.Direction)direction;
			
			var canDoEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.ActionCanDoEvadeArgs.Instance;
			canDoEvadeArgs.State = g.state;
			canDoEvadeArgs.Combat = combat;
			canDoEvadeArgs.Direction = typedDirection;

			return FilterEnabled(g.state, combat, typedDirection, Instance.ActionEntries).FirstOrDefault(entry =>
			{
				if (!entry.Action.CanDoEvadeAction(canDoEvadeArgs))
					return false;
				
				var paymentOptionCanPayForEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionCanPayForEvadeArgs.Instance;
				paymentOptionCanPayForEvadeArgs.State = g.state;
				paymentOptionCanPayForEvadeArgs.Combat = combat;
				paymentOptionCanPayForEvadeArgs.Direction = typedDirection;
				paymentOptionCanPayForEvadeArgs.Entry = entry;

				if (!FilterEnabled(g.state, combat, typedDirection, entry, entry.PaymentOptions).Any(paymentOption => paymentOption.CanPayForEvade(paymentOptionCanPayForEvadeArgs)))
					return false;

				return true;
			});
		}
	}
	
	private static bool Combat_DoEvade_Prefix(Combat __instance, G g, int dir)
	{
		if (!__instance.PlayerCanAct(g.state))
			return false;
		
		var typedDirection = (IKokoroApi.IV2.IEvadeHookApi.Direction)dir;

		foreach (var entry in FilterEnabled(g.state, __instance, typedDirection, Instance.ActionEntries))
		{
			var canDoEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.ActionCanDoEvadeArgs.Instance;
			canDoEvadeArgs.State = g.state;
			canDoEvadeArgs.Combat = __instance;
			canDoEvadeArgs.Direction = typedDirection;

			if (!entry.Action.CanDoEvadeAction(canDoEvadeArgs))
				continue;
			
			var preconditionIsEvadeAllowedArgs = ApiImplementation.V2Api.EvadeHookApi.PreconditionIsEvadeAllowedArgs.Instance;
			preconditionIsEvadeAllowedArgs.State = g.state;
			preconditionIsEvadeAllowedArgs.Combat = __instance;
			preconditionIsEvadeAllowedArgs.Direction = typedDirection;
			preconditionIsEvadeAllowedArgs.Entry = entry;

			foreach (var precondition in FilterEnabled(g.state, __instance, typedDirection, entry, entry.Preconditions))
			{
				var preconditionResult = precondition.IsEvadeAllowed(preconditionIsEvadeAllowedArgs);
				if (!preconditionResult.IsAllowed)
				{
					if (preconditionResult.ShakeShipOnFail)
					{
						Audio.Play(Event.Status_PowerDown);
						g.state.ship.shake += 1.0;
					}
					__instance.Queue(preconditionResult.ActionsOnFail);
					return false;
				}
			}

			foreach (var paymentOption in FilterEnabled(g.state, __instance, typedDirection, entry, entry.PaymentOptions))
			{
				var paymentOptionCanPayForEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionCanPayForEvadeArgs.Instance;
				paymentOptionCanPayForEvadeArgs.State = g.state;
				paymentOptionCanPayForEvadeArgs.Combat = __instance;
				paymentOptionCanPayForEvadeArgs.Direction = typedDirection;
				paymentOptionCanPayForEvadeArgs.Entry = entry;

				if (!paymentOption.CanPayForEvade(paymentOptionCanPayForEvadeArgs))
					continue;
				
				var paymentOptionProvideEvadePaymentActionsArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionProvideEvadePaymentActionsArgs.Instance;
				paymentOptionProvideEvadePaymentActionsArgs.State = g.state;
				paymentOptionProvideEvadePaymentActionsArgs.Combat = __instance;
				paymentOptionProvideEvadePaymentActionsArgs.Direction = typedDirection;
				paymentOptionProvideEvadePaymentActionsArgs.Entry = entry;

				var paymentActions = paymentOption.ProvideEvadePaymentActions(paymentOptionProvideEvadePaymentActionsArgs);
				
				var actionProvideEvadeActionsArgs = ApiImplementation.V2Api.EvadeHookApi.ActionProvideEvadeActionsArgs.Instance;
				actionProvideEvadeActionsArgs.State = g.state;
				actionProvideEvadeActionsArgs.Combat = __instance;
				actionProvideEvadeActionsArgs.Direction = typedDirection;
				actionProvideEvadeActionsArgs.PaymentOption = paymentOption;

				var evadeActions = entry.Action.ProvideEvadeActions(actionProvideEvadeActionsArgs);

				List<CardAction> allActions = [.. paymentActions, .. evadeActions];
				__instance.Queue(allActions);
				
				var hookAfterEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.HookAfterEvadeArgs.Instance;
				hookAfterEvadeArgs.State = g.state;
				hookAfterEvadeArgs.Combat = __instance;
				hookAfterEvadeArgs.Direction = typedDirection;
				hookAfterEvadeArgs.Entry = entry;
				hookAfterEvadeArgs.PaymentOption = paymentOption;
				hookAfterEvadeArgs.QueuedActions = allActions;
				
				foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, g.state.EnumerateAllArtifacts()))
					hook.AfterEvade(hookAfterEvadeArgs);
				
				return false;
			}
		}

		return false;
	}
}

internal sealed class DefaultEvadeAction : IKokoroApi.IV2.IEvadeHookApi.IEvadeAction
{
	public static DefaultEvadeAction Instance { get; private set; } = new();
	
	private DefaultEvadeAction() { }
	
	public bool CanDoEvadeAction(IKokoroApi.IV2.IEvadeHookApi.IEvadeAction.ICanDoEvadeArgs args)
		=> true;

	public IReadOnlyList<CardAction> ProvideEvadeActions(IKokoroApi.IV2.IEvadeHookApi.IEvadeAction.IProvideEvadeActionsArgs args)
		=> [new AMove { targetPlayer = true, dir = (int)args.Direction, fromEvade = true }];
}

internal sealed class DefaultEvadePaymentOption : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption
{
	public static DefaultEvadePaymentOption Instance { get; private set; } = new();
	
	private DefaultEvadePaymentOption() { }
	
	public bool CanPayForEvade(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.ICanPayForEvadeArgs args)
		=> args.State.ship.Get(Status.evade) > 0;

	public IReadOnlyList<CardAction> ProvideEvadePaymentActions(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IProvideEvadePaymentActionsArgs args)
	{
		args.State.ship.Add(Status.evade, -1);
		return [];
	}
}

internal sealed class DebugEvadePaymentOption : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption
{
	public static DebugEvadePaymentOption Instance { get; private set; } = new();
	
	private DebugEvadePaymentOption() { }
	
	public bool CanPayForEvade(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.ICanPayForEvadeArgs args)
		=> FeatureFlags.Debug && Input.shift;

	public IReadOnlyList<CardAction> ProvideEvadePaymentActions(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IProvideEvadePaymentActionsArgs args)
		=> [];
}

internal sealed class LockdownEvadePrecondition : IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition
{
	public static LockdownEvadePrecondition Instance { get; private set; } = new();
	
	private LockdownEvadePrecondition() { }
	
	public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult IsEvadeAllowed(IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IIsEvadeAllowedArgs args)
	{
		var isLockdowned = args.State.ship.Get(Status.lockdown) > 0;
		return ModEntry.Instance.Api.V2.EvadeHook.MakePreconditionResult(!isLockdowned);
	}
}

internal sealed class AnchorEvadePrecondition : IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition
{
	public static AnchorEvadePrecondition Instance { get; private set; } = new();
	
	private AnchorEvadePrecondition() { }
	
	public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult IsEvadeAllowed(IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IIsEvadeAllowedArgs args)
	{
		var hasAnchor = args.Combat.hand.Any(c => c is TrashAnchor);
		return ModEntry.Instance.Api.V2.EvadeHook.MakePreconditionResult(!hasAnchor);
	}
}

internal sealed class DebugEvadeHook : IKokoroApi.IV2.IEvadeHookApi.IHook
{
	public static DebugEvadeHook Instance { get; private set; } = new();
	
	private DebugEvadeHook() { }
	
	public bool IsEvadePreconditionEnabled(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePaymentOptionEnabledArgs args)
	{
		var isDebug = FeatureFlags.Debug && Input.shift;
		return !isDebug || args.Entry == ModEntry.Instance.Api.V2.EvadeHook.DefaultAction;
	}
}

internal sealed class FakeVanillaEvadeV1Hook : IEvadeHook
{
	public static FakeVanillaEvadeV1Hook Instance { get; private set; } = new();
	
	private FakeVanillaEvadeV1Hook() { }
}

internal sealed class FakeVanillaDebugEvadeV1Hook : IEvadeHook
{
	public static FakeVanillaDebugEvadeV1Hook Instance { get; private set; } = new();
	
	private FakeVanillaDebugEvadeV1Hook() { }
}