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

	public IDroneShiftHook VanillaDroneShiftHook
	{
		get
		{
			if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(VanillaDroneShiftHook))))
				ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(VanillaDroneShiftHook));
			return FakeVanillaDroneShiftV1Hook.Instance;
		}
	}

	public IDroneShiftHook VanillaDebugDroneShiftHook
	{
		get
		{
			if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(VanillaDebugDroneShiftHook))))
				ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(VanillaDebugDroneShiftHook));
			return FakeVanillaDebugDroneShiftV1Hook.Instance;
		}
	}

	public void RegisterDroneShiftHook(IDroneShiftHook hook, double priority)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(RegisterDroneShiftHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(RegisterDroneShiftHook));
	}

	public void UnregisterDroneShiftHook(IDroneShiftHook hook)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(UnregisterDroneShiftHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(UnregisterDroneShiftHook));
	}

	public bool IsDroneShiftPossible(State state, Combat combat, int direction, DroneShiftHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(IsDroneShiftPossible))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(IsDroneShiftPossible));
		return false;
	}

	public bool IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(IsDroneShiftPossible))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(IsDroneShiftPossible));
		return false;
	}

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, int direction, DroneShiftHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(GetDroneShiftHandlingHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(GetDroneShiftHandlingHook));
		return null;
	}

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, DroneShiftHookContext context)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(GetDroneShiftHandlingHook))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(GetDroneShiftHandlingHook));
		return null;
	}

	public void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook)
	{
		if (LoggedBrokenV1ApiCalls.Add((Manifest.Name, nameof(AfterDroneShift))))
			ModEntry.Instance.Logger!.LogError("`{ModName}` attempted to access `{V1Api}` V1 API, but it can no longer be used - the mod may not work correctly.", Manifest.Name, nameof(AfterDroneShift));
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IDroneShiftHookApi DroneShiftHook { get; } = new DroneShiftHookApi();
		
		public sealed class DroneShiftHookApi : IKokoroApi.IV2.IDroneShiftHookApi
		{
			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry DefaultAction
				=> DroneShiftManager.Instance.DefaultActionEntry;
			
			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption DefaultActionPaymentOption
				=> DefaultDroneShiftPaymentOption.Instance;
			
			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption DebugActionPaymentOption
				=> DebugDroneShiftPaymentOption.Instance;

			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry RegisterAction(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction action, double priority = 0)
			{
				var entry = new DroneShiftActionEntry(action);
				DroneShiftManager.Instance.ActionEntries.Add(entry, priority);
				return entry;
			}

			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IResult MakePreconditionResult(bool isAllowed)
				=> new DroneShiftPreconditionResult(isAllowed);

			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IResult MakePostconditionResult(bool isAllowed)
				=> new DroneShiftPostconditionResult(isAllowed);

			public void RegisterHook(IKokoroApi.IV2.IDroneShiftHookApi.IHook hook, double priority = 0)
				=> DroneShiftManager.Instance.HookManager.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IDroneShiftHookApi.IHook hook)
				=> DroneShiftManager.Instance.HookManager.Unregister(hook);
			
			internal sealed class DroneShiftActionEntry(
				IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction action
			) : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry
			{
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction Action { get; } = action;

				public IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption> PaymentOptions
					=> PaymentOptionsOrderedList;

				public IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition> Preconditions
					=> PreconditionsOrderedList;

				public IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition> Postconditions
					=> PostconditionsOrderedList;

				private readonly OrderedList<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption, double> PaymentOptionsOrderedList = new(ascending: false);
				private readonly OrderedList<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition, double> PreconditionsOrderedList = new(ascending: false);
				private readonly OrderedList<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition, double> PostconditionsOrderedList = new(ascending: false);

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry RegisterPaymentOption(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption paymentOption, double priority = 0)
				{
					PaymentOptionsOrderedList.Add(paymentOption, priority);
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry UnregisterPaymentOption(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption paymentOption)
				{
					PaymentOptionsOrderedList.Remove(paymentOption);
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry RegisterPrecondition(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition precondition, double priority = 0)
				{
					PreconditionsOrderedList.Add(precondition, priority);
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry UnregisterPrecondition(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition precondition)
				{
					PreconditionsOrderedList.Remove(precondition);
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry RegisterPostcondition(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition postcondition, double priority = 0)
				{
					PostconditionsOrderedList.Add(postcondition, priority);
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry UnregisterPostcondition(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition postcondition)
				{
					PostconditionsOrderedList.Remove(postcondition);
					return this;
				}
			}
			
			internal sealed class DroneShiftPreconditionResult(
				bool isAllowed
			) : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IResult
			{
				public bool IsAllowed { get; set; } = isAllowed;
				public bool ShakeShipOnFail { get; set; } = true;
				public IList<CardAction> ActionsOnFail { get; set; } = [];
				
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IResult SetIsAllowed(bool value)
				{
					IsAllowed = value;
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IResult SetShakeShipOnFail(bool value)
				{
					ShakeShipOnFail = value;
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IResult SetActionsOnFail(IList<CardAction> value)
				{
					ActionsOnFail = value;
					return this;
				}
			}
			
			internal sealed class DroneShiftPostconditionResult(
				bool isAllowed
			) : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IResult
			{
				public bool IsAllowed { get; set; } = isAllowed;
				public bool ShakeShipOnFail { get; set; } = true;
				public IList<CardAction> ActionsOnFail { get; set; } = [];
				
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IResult SetIsAllowed(bool value)
				{
					IsAllowed = value;
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IResult SetShakeShipOnFail(bool value)
				{
					ShakeShipOnFail = value;
					return this;
				}

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IResult SetActionsOnFail(IList<CardAction> value)
				{
					ActionsOnFail = value;
					return this;
				}
			}
			
			internal sealed class ActionCanDoDroneShiftArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction.ICanDoDroneShiftArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ActionCanDoDroneShiftArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
			}
			
			internal sealed class ActionProvideDroneShiftActionsArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction.IProvideDroneShiftActionsArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ActionProvideDroneShiftActionsArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
			}
			
			internal sealed class PaymentOptionCanPayForDroneShiftArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.ICanPayForDroneShiftArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PaymentOptionCanPayForDroneShiftArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PaymentOptionProvideDroneShiftPaymentActionsArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IProvideDroneShiftPaymentActionsArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PaymentOptionProvideDroneShiftPaymentActionsArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PreconditionIsDroneShiftAllowedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IIsDroneShiftAllowedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PreconditionIsDroneShiftAllowedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PostconditionIsDroneShiftAllowedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IIsDroneShiftAllowedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PostconditionIsDroneShiftAllowedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
			}
			
			internal sealed class HookIsDroneShiftActionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftActionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsDroneShiftActionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class HookIsDroneShiftPaymentOptionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPaymentOptionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsDroneShiftPaymentOptionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
			}
			
			internal sealed class HookIsDroneShiftPreconditionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPreconditionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsDroneShiftPreconditionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition Precondition { get; internal set; } = null!;
			}
			
			internal sealed class HookIsDroneShiftPostconditionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPostconditionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsDroneShiftPostconditionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition Postcondition { get; internal set; } = null!;
			}
			
			internal sealed class HookDroneShiftPreconditionFailedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IDroneShiftPreconditionFailedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookDroneShiftPreconditionFailedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition Precondition { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
			}
			
			internal sealed class HookDroneShiftPostconditionFailedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IDroneShiftPostconditionFailedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookDroneShiftPostconditionFailedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition Postcondition { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
			}
			
			internal sealed class HookAfterDroneShiftArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IAfterDroneShiftArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookAfterDroneShiftArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class DroneShiftManager
{
	internal static readonly DroneShiftManager Instance = new();

	internal readonly HookManager<IKokoroApi.IV2.IDroneShiftHookApi.IHook> HookManager;
	internal readonly OrderedList<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry, double> ActionEntries;
	internal readonly IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry DefaultActionEntry;
	
	private DroneShiftManager()
	{
		HookManager = new(ModEntry.Instance.Package.Manifest.UniqueName);
		ActionEntries = new(ascending: false);
		
		var defaultActionEntry = new ApiImplementation.V2Api.DroneShiftHookApi.DroneShiftActionEntry(DefaultDroneShiftAction.Instance)
			.RegisterPaymentOption(DefaultDroneShiftPaymentOption.Instance)
			.RegisterPaymentOption(DebugDroneShiftPaymentOption.Instance, 1_000_000_000);
		DefaultActionEntry = defaultActionEntry;
		ActionEntries.Add(defaultActionEntry, 0);
		
		HookManager.Register(DebugDroneShiftHook.Instance, 1_000_000_000);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDroneShiftButtons)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDroneShiftButtons_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DoDroneShift)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DoDroneShift_Prefix))
		);
	}

	private static IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IDroneShiftHookApi.Direction direction,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry> enumerable
	)
	{
		var args = ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftActionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(e =>
		{
			args.Entry = e;
			return hooks.All(h => h.IsDroneShiftActionEnabled(args));
		});
	}

	private static IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IDroneShiftHookApi.Direction direction,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption> enumerable
	)
	{
		var args = ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftPaymentOptionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		args.Entry = entry;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(paymentOption =>
		{
			args.PaymentOption = paymentOption;
			return hooks.All(h => h.IsDroneShiftPaymentOptionEnabled(args));
		});
	}

	private static IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IDroneShiftHookApi.Direction direction,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition> enumerable
	)
	{
		var args = ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftPreconditionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		args.Entry = entry;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(precondition =>
		{
			args.Precondition = precondition;
			return hooks.All(h => h.IsDroneShiftPreconditionEnabled(args));
		});
	}

	private static IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IDroneShiftHookApi.Direction direction,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption paymentOption,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition> enumerable
	)
	{
		var args = ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftPostconditionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		args.Entry = entry;
		args.PaymentOption = paymentOption;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(postcondition =>
		{
			args.Postcondition = postcondition;
			return hooks.All(h => h.IsDroneShiftPostconditionEnabled(args));
		});
	}
	
	private static IEnumerable<CodeInstruction> Combat_RenderDroneShiftButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
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
					ILMatches.LdcI4((int)Status.droneShift),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.Replace(new CodeInstruction(OpCodes.Nop).WithLabels(labels))
				.Find([
					ILMatches.Ldloc<Combat>(originalMethod),
					ILMatches.Ldfld("stuff"),
					ILMatches.Call("get_Count"),
					ILMatches.Brfalse
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Call("Any"),
					ILMatches.Brtrue,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.Remove()
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldarg(1).Anchor(out var gPointer1),
					ILMatches.Stloc<G>(originalMethod),
					ILMatches.LdcI4((int)StableUK.btn_moveDrones_left),
					ILMatches.Call("op_Implicit")
				])
				.Anchors()
				.PointerMatcher(gPointer1)
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldc_I4, -1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brfalse, leftEndLabel),
					new CodeInstruction(OpCodes.Ldarg_1)
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Ldarg(1),
					ILMatches.Stloc<G>(originalMethod),
					ILMatches.LdcI4((int)StableUK.btn_moveDrones_right),
					ILMatches.Call("op_Implicit")
				])
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Encompass(SequenceMatcherEncompassDirection.Before, 3)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(leftEndLabel),
					new CodeInstruction(OpCodes.Ldc_I4, 1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRender))),
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

	private static bool Combat_RenderDroneShiftButtons_Transpiler_ShouldRender(G g, int direction)
	{
		if (g.state.route is not Combat combat)
			return false;

		return GetActionEntry() is not null;

		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry? GetActionEntry()
		{
			var typedDirection = (IKokoroApi.IV2.IDroneShiftHookApi.Direction)direction;
			
			var canDoDroneShiftArgs = ApiImplementation.V2Api.DroneShiftHookApi.ActionCanDoDroneShiftArgs.Instance;
			canDoDroneShiftArgs.State = g.state;
			canDoDroneShiftArgs.Combat = combat;
			canDoDroneShiftArgs.Direction = typedDirection;

			return FilterEnabled(g.state, combat, typedDirection, Instance.ActionEntries).FirstOrDefault(entry =>
			{
				if (!entry.Action.CanDoDroneShiftAction(canDoDroneShiftArgs))
					return false;
				
				var paymentOptionCanPayForDroneShiftArgs = ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionCanPayForDroneShiftArgs.Instance;
				paymentOptionCanPayForDroneShiftArgs.State = g.state;
				paymentOptionCanPayForDroneShiftArgs.Combat = combat;
				paymentOptionCanPayForDroneShiftArgs.Direction = typedDirection;
				paymentOptionCanPayForDroneShiftArgs.Entry = entry;

				if (!FilterEnabled(g.state, combat, typedDirection, entry, entry.PaymentOptions).Any(paymentOption => paymentOption.CanPayForDroneShift(paymentOptionCanPayForDroneShiftArgs)))
					return false;

				return true;
			});
		}
	}
	
	private static bool Combat_DoDroneShift_Prefix(Combat __instance, G g, int dir)
	{
		if (!__instance.PlayerCanAct(g.state))
			return false;
		
		var typedDirection = (IKokoroApi.IV2.IDroneShiftHookApi.Direction)dir;

		foreach (var entry in FilterEnabled(g.state, __instance, typedDirection, Instance.ActionEntries))
		{
			var canDoDroneShiftArgs = ApiImplementation.V2Api.DroneShiftHookApi.ActionCanDoDroneShiftArgs.Instance;
			canDoDroneShiftArgs.State = g.state;
			canDoDroneShiftArgs.Combat = __instance;
			canDoDroneShiftArgs.Direction = typedDirection;

			if (!entry.Action.CanDoDroneShiftAction(canDoDroneShiftArgs))
				continue;
			
			var preconditionIsDroneShiftAllowedArgs = ApiImplementation.V2Api.DroneShiftHookApi.PreconditionIsDroneShiftAllowedArgs.Instance;
			preconditionIsDroneShiftAllowedArgs.State = g.state;
			preconditionIsDroneShiftAllowedArgs.Combat = __instance;
			preconditionIsDroneShiftAllowedArgs.Direction = typedDirection;
			preconditionIsDroneShiftAllowedArgs.Entry = entry;

			foreach (var precondition in FilterEnabled(g.state, __instance, typedDirection, entry, entry.Preconditions))
			{
				var preconditionResult = precondition.IsDroneShiftAllowed(preconditionIsDroneShiftAllowedArgs);
				if (!preconditionResult.IsAllowed)
				{
					if (preconditionResult.ShakeShipOnFail)
					{
						Audio.Play(Event.Status_PowerDown);
						g.state.ship.shake += 1.0;
					}
					
					__instance.Queue(preconditionResult.ActionsOnFail);
					
					var hookDroneShiftPreconditionFailedArgs = ApiImplementation.V2Api.DroneShiftHookApi.HookDroneShiftPreconditionFailedArgs.Instance;
					hookDroneShiftPreconditionFailedArgs.State = g.state;
					hookDroneShiftPreconditionFailedArgs.Combat = __instance;
					hookDroneShiftPreconditionFailedArgs.Direction = typedDirection;
					hookDroneShiftPreconditionFailedArgs.Entry = entry;
					hookDroneShiftPreconditionFailedArgs.Precondition = precondition;
					hookDroneShiftPreconditionFailedArgs.QueuedActions = preconditionResult.ActionsOnFail.ToList();
				
					foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, g.state.EnumerateAllArtifacts()))
						hook.DroneShiftPreconditionFailed(hookDroneShiftPreconditionFailedArgs);
					
					return false;
				}
			}

			foreach (var paymentOption in FilterEnabled(g.state, __instance, typedDirection, entry, entry.PaymentOptions))
			{
				var paymentOptionCanPayForDroneShiftArgs = ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionCanPayForDroneShiftArgs.Instance;
				paymentOptionCanPayForDroneShiftArgs.State = g.state;
				paymentOptionCanPayForDroneShiftArgs.Combat = __instance;
				paymentOptionCanPayForDroneShiftArgs.Direction = typedDirection;
				paymentOptionCanPayForDroneShiftArgs.Entry = entry;

				if (!paymentOption.CanPayForDroneShift(paymentOptionCanPayForDroneShiftArgs))
					continue;
				
				var paymentOptionProvideDroneShiftPaymentActionsArgs = ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionProvideDroneShiftPaymentActionsArgs.Instance;
				paymentOptionProvideDroneShiftPaymentActionsArgs.State = g.state;
				paymentOptionProvideDroneShiftPaymentActionsArgs.Combat = __instance;
				paymentOptionProvideDroneShiftPaymentActionsArgs.Direction = typedDirection;
				paymentOptionProvideDroneShiftPaymentActionsArgs.Entry = entry;

				var paymentActions = paymentOption.ProvideDroneShiftPaymentActions(paymentOptionProvideDroneShiftPaymentActionsArgs);
				
				var postconditionIsDroneShiftAllowedArgs = ApiImplementation.V2Api.DroneShiftHookApi.PostconditionIsDroneShiftAllowedArgs.Instance;
				postconditionIsDroneShiftAllowedArgs.State = g.state;
				postconditionIsDroneShiftAllowedArgs.Combat = __instance;
				postconditionIsDroneShiftAllowedArgs.Direction = typedDirection;
				postconditionIsDroneShiftAllowedArgs.Entry = entry;
				postconditionIsDroneShiftAllowedArgs.PaymentOption = paymentOption;
				
				foreach (var postcondition in FilterEnabled(g.state, __instance, typedDirection, entry, paymentOption, entry.Postconditions))
				{
					var postconditionResult = postcondition.IsDroneShiftAllowed(postconditionIsDroneShiftAllowedArgs);
					if (!postconditionResult.IsAllowed)
					{
						if (postconditionResult.ShakeShipOnFail)
						{
							Audio.Play(Event.Status_PowerDown);
							g.state.ship.shake += 1.0;
						}
						
						List<CardAction> queuedActions = [.. paymentActions, .. postconditionResult.ActionsOnFail];
						__instance.Queue(queuedActions);
						
						var hookDroneShiftPostconditionFailedArgs = ApiImplementation.V2Api.DroneShiftHookApi.HookDroneShiftPostconditionFailedArgs.Instance;
						hookDroneShiftPostconditionFailedArgs.State = g.state;
						hookDroneShiftPostconditionFailedArgs.Combat = __instance;
						hookDroneShiftPostconditionFailedArgs.Direction = typedDirection;
						hookDroneShiftPostconditionFailedArgs.Entry = entry;
						hookDroneShiftPostconditionFailedArgs.PaymentOption = paymentOption;
						hookDroneShiftPostconditionFailedArgs.Postcondition = postcondition;
						hookDroneShiftPostconditionFailedArgs.QueuedActions = queuedActions;
				
						foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, g.state.EnumerateAllArtifacts()))
							hook.DroneShiftPostconditionFailed(hookDroneShiftPostconditionFailedArgs);
						
						return false;
					}
				}
				
				var actionProvideDroneShiftActionsArgs = ApiImplementation.V2Api.DroneShiftHookApi.ActionProvideDroneShiftActionsArgs.Instance;
				actionProvideDroneShiftActionsArgs.State = g.state;
				actionProvideDroneShiftActionsArgs.Combat = __instance;
				actionProvideDroneShiftActionsArgs.Direction = typedDirection;
				actionProvideDroneShiftActionsArgs.PaymentOption = paymentOption;

				var droneShiftActions = entry.Action.ProvideDroneShiftActions(actionProvideDroneShiftActionsArgs);

				List<CardAction> allActions = [.. paymentActions, .. droneShiftActions];
				__instance.Queue(allActions);
				
				var hookAfterDroneShiftArgs = ApiImplementation.V2Api.DroneShiftHookApi.HookAfterDroneShiftArgs.Instance;
				hookAfterDroneShiftArgs.State = g.state;
				hookAfterDroneShiftArgs.Combat = __instance;
				hookAfterDroneShiftArgs.Direction = typedDirection;
				hookAfterDroneShiftArgs.Entry = entry;
				hookAfterDroneShiftArgs.PaymentOption = paymentOption;
				hookAfterDroneShiftArgs.QueuedActions = allActions;
				
				foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, g.state.EnumerateAllArtifacts()))
					hook.AfterDroneShift(hookAfterDroneShiftArgs);
				
				return false;
			}
		}

		return false;
	}
}

internal sealed class DefaultDroneShiftAction : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction
{
	public static DefaultDroneShiftAction Instance { get; private set; } = new();
	
	private DefaultDroneShiftAction() { }
	
	public bool CanDoDroneShiftAction(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction.ICanDoDroneShiftArgs args)
		=> args.Combat.stuff.Values.Any(o => !o.Immovable());

	public IReadOnlyList<CardAction> ProvideDroneShiftActions(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction.IProvideDroneShiftActionsArgs args)
		=> [new ADroneMove { dir = (int)args.Direction }];
}

internal sealed class DefaultDroneShiftPaymentOption : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption
{
	public static DefaultDroneShiftPaymentOption Instance { get; private set; } = new();
	
	private DefaultDroneShiftPaymentOption() { }
	
	public bool CanPayForDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.ICanPayForDroneShiftArgs args)
		=> args.State.ship.Get(Status.droneShift) > 0;

	public IReadOnlyList<CardAction> ProvideDroneShiftPaymentActions(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IProvideDroneShiftPaymentActionsArgs args)
	{
		args.State.ship.Add(Status.droneShift, -1);
		return [];
	}
}

internal sealed class DebugDroneShiftPaymentOption : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption
{
	public static DebugDroneShiftPaymentOption Instance { get; private set; } = new();
	
	private DebugDroneShiftPaymentOption() { }
	
	public bool CanPayForDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.ICanPayForDroneShiftArgs args)
		=> FeatureFlags.Debug && Input.shift;

	public IReadOnlyList<CardAction> ProvideDroneShiftPaymentActions(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IProvideDroneShiftPaymentActionsArgs args)
		=> [];
}

internal sealed class DebugDroneShiftHook : IKokoroApi.IV2.IDroneShiftHookApi.IHook
{
	public static DebugDroneShiftHook Instance { get; private set; } = new();
	
	private DebugDroneShiftHook() { }
	
	public bool IsDroneShiftPreconditionEnabled(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPreconditionEnabledArgs args)
	{
		var isDebug = FeatureFlags.Debug && Input.shift;
		return !isDebug || args.Entry == ModEntry.Instance.Api.V2.DroneShiftHook.DefaultAction;
	}
}

internal sealed class FakeVanillaDroneShiftV1Hook : IDroneShiftHook
{
	public static FakeVanillaDroneShiftV1Hook Instance { get; private set; } = new();
	
	private FakeVanillaDroneShiftV1Hook() { }
}

internal sealed class FakeVanillaDebugDroneShiftV1Hook : IDroneShiftHook
{
	public static FakeVanillaDebugDroneShiftV1Hook Instance { get; private set; } = new();
	
	private FakeVanillaDebugDroneShiftV1Hook() { }
}