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

			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry? GetNextAction(State state, Combat combat, IKokoroApi.IV2.IDroneShiftHookApi.Direction direction)
				=> DroneShiftManager.Instance.GetNextAction(state, combat, direction, true);

			public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry? GetNextAction(State state, Combat combat, IKokoroApi.IV2.IDroneShiftHookApi.Direction direction, bool forRendering)
				=> DroneShiftManager.Instance.GetNextAction(state, combat, direction, forRendering);

			public void DidHoverButton(State state, Combat combat, IKokoroApi.IV2.IDroneShiftHookApi.Direction direction)
				=> DroneShiftManager.Instance.DidHoverButton(state, combat, direction);

			public IKokoroApi.IV2.IDroneShiftHookApi.IRunActionResult RunNextAction(State state, Combat combat, IKokoroApi.IV2.IDroneShiftHookApi.Direction direction)
				=> DroneShiftManager.Instance.RunNextAction(state, combat, direction);

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
					=> PreconditionsOrderedList.Entries
						.Concat(InheritsPreconditionsFrom.SelectMany(e => ((DroneShiftActionEntry)e).PreconditionsOrderedList.Entries))
						.OrderByDescending(e => e.OrderingValue)
						.Select(e => e.Element);

				public IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition> Postconditions
					=> PostconditionsOrderedList.Entries
						.Concat(InheritsPreconditionsFrom.SelectMany(e => ((DroneShiftActionEntry)e).PostconditionsOrderedList.Entries))
						.OrderByDescending(e => e.OrderingValue)
						.Select(e => e.Element);

				private readonly OrderedList<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption, double> PaymentOptionsOrderedList = new(ascending: false);
				private readonly OrderedList<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition, double> PreconditionsOrderedList = new(ascending: false);
				private readonly OrderedList<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition, double> PostconditionsOrderedList = new(ascending: false);
				private readonly List<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry> InheritsPreconditionsFrom = [];
				private readonly List<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry> InheritsPostconditionsFrom = [];

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

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry InheritPreconditions(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry)
				{
					if (InheritsPreconditionsFrom.Contains(entry))
						return this;
					InheritsPreconditionsFrom.Add(entry);
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

				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry InheritPostconditions(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry)
				{
					if (InheritsPostconditionsFrom.Contains(entry))
						return this;
					InheritsPostconditionsFrom.Add(entry);
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
			
			internal sealed class RunActionResult : IKokoroApi.IV2.IDroneShiftHookApi.IRunActionResult
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry? Entry { get; internal set; }
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption? PaymentOption { get; internal set; }
				public IKokoroApi.IV2.IDroneShiftHookApi.IHook.IDroneShiftPreconditionFailedArgs? PreconditionFailed { get; internal set; }
				public IKokoroApi.IV2.IDroneShiftHookApi.IHook.IDroneShiftPostconditionFailedArgs? PostconditionFailed { get; internal set; }
				public IKokoroApi.IV2.IDroneShiftHookApi.IHook.IAfterDroneShiftArgs? Success { get; internal set; }
			}
			
			internal sealed class ActionCanDoDroneShiftArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction.ICanDoDroneShiftArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
			}
			
			internal sealed class ActionProvideDroneShiftActionsArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction.IProvideDroneShiftActionsArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
			}
			
			internal sealed class ActionDroneShiftButtonHoveredArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftAction.IDroneShiftButtonHoveredArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
			}
			
			internal sealed class PaymentOptionCanPayForDroneShiftArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.ICanPayForDroneShiftArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PaymentOptionProvideDroneShiftPaymentActionsArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IProvideDroneShiftPaymentActionsArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PaymentOptionDroneShiftButtonHoveredArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IDroneShiftButtonHoveredArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
			}
			
			internal sealed class PreconditionIsDroneShiftAllowedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IIsDroneShiftAllowedArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class PreconditionDroneShiftButtonHoveredArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IDroneShiftButtonHoveredArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition.IResult Result { get; internal set; } = null!;
			}
			
			internal sealed class PostconditionIsDroneShiftAllowedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IIsDroneShiftAllowedArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class PostconditionDroneShiftButtonHoveredArgs : IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IDroneShiftButtonHoveredArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition.IResult Result { get; internal set; } = null!;
			}
			
			internal sealed class HookIsDroneShiftActionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftActionEnabledArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class HookIsDroneShiftPaymentOptionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPaymentOptionEnabledArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class HookIsDroneShiftPreconditionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPreconditionEnabledArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition Precondition { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class HookIsDroneShiftPostconditionEnabledArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPostconditionEnabledArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition Postcondition { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class HookDroneShiftPreconditionFailedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IDroneShiftPreconditionFailedArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition Precondition { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
			}
			
			internal sealed class HookDroneShiftPostconditionFailedArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IDroneShiftPostconditionFailedArgs
			{
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
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption PaymentOption { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
			}
			
			internal sealed class HookShouldShowDroneShiftButtonArgs : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IShouldShowDroneShiftButtonArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IDroneShiftHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
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
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDroneShiftButtons_Postfix)),
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
		bool forRendering,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry> enumerable
	)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftActionEnabledArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Direction = direction;
			args.ForRendering = forRendering;

			var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
			return enumerable.Where(e =>
			{
				args.Entry = e;
				return hooks.All(h => h.IsDroneShiftActionEnabled(args));
			});
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	private static IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IDroneShiftHookApi.Direction direction,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry,
		bool forRendering,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption> enumerable
	)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftPaymentOptionEnabledArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Direction = direction;
			args.Entry = entry;
			args.ForRendering = forRendering;

			var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
			return enumerable.Where(paymentOption =>
			{
				args.PaymentOption = paymentOption;
				return hooks.All(h => h.IsDroneShiftPaymentOptionEnabled(args));
			});
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	private static IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IDroneShiftHookApi.Direction direction,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry,
		bool forRendering,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPrecondition> enumerable
	)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftPreconditionEnabledArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Direction = direction;
			args.Entry = entry;
			args.ForRendering = forRendering;

			var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
			return enumerable.Where(precondition =>
			{
				args.Precondition = precondition;
				return hooks.All(h => h.IsDroneShiftPreconditionEnabled(args));
			});
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	private static IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IDroneShiftHookApi.Direction direction,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry entry,
		IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption paymentOption,
		bool forRendering,
		IEnumerable<IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPostcondition> enumerable
	)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookIsDroneShiftPostconditionEnabledArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Direction = direction;
			args.Entry = entry;
			args.PaymentOption = paymentOption;
			args.ForRendering = forRendering;

			var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
			return enumerable.Where(postcondition =>
			{
				args.Postcondition = postcondition;
				return hooks.All(h => h.IsDroneShiftPostconditionEnabled(args));
			});
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}
	
	public IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftActionEntry? GetNextAction(State state, Combat combat, IKokoroApi.IV2.IDroneShiftHookApi.Direction direction, bool forRendering)
	{
		var canDoDroneShiftArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.ActionCanDoDroneShiftArgs>();
		var paymentOptionCanPayForDroneShiftArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionCanPayForDroneShiftArgs>();
		var preconditionIsDroneShiftAllowedArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PreconditionIsDroneShiftAllowedArgs>();

		try
		{
			canDoDroneShiftArgs.State = state;
			canDoDroneShiftArgs.Combat = combat;
			canDoDroneShiftArgs.Direction = direction;

			return FilterEnabled(state, combat, direction, forRendering, Instance.ActionEntries).FirstOrDefault(entry =>
			{
				if (!entry.Action.CanDoDroneShiftAction(canDoDroneShiftArgs))
					return false;

				paymentOptionCanPayForDroneShiftArgs.State = state;
				paymentOptionCanPayForDroneShiftArgs.Combat = combat;
				paymentOptionCanPayForDroneShiftArgs.Direction = direction;
				paymentOptionCanPayForDroneShiftArgs.Entry = entry;

				if (!FilterEnabled(state, combat, direction, entry, forRendering, entry.PaymentOptions).Any(paymentOption => paymentOption.CanPayForDroneShift(paymentOptionCanPayForDroneShiftArgs)))
					return false;

				preconditionIsDroneShiftAllowedArgs.State = state;
				preconditionIsDroneShiftAllowedArgs.Combat = combat;
				preconditionIsDroneShiftAllowedArgs.Direction = direction;
				preconditionIsDroneShiftAllowedArgs.Entry = entry;
				preconditionIsDroneShiftAllowedArgs.ForRendering = true;

				if (FilterEnabled(state, combat, direction, entry, forRendering, entry.Preconditions).Any(precondition => !precondition.IsDroneShiftAllowed(preconditionIsDroneShiftAllowedArgs).IsAllowed))
					return false;

				return true;
			});
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(canDoDroneShiftArgs);
			ModEntry.Instance.ArgsPool.Return(paymentOptionCanPayForDroneShiftArgs);
			ModEntry.Instance.ArgsPool.Return(preconditionIsDroneShiftAllowedArgs);
		}
	}

	public void DidHoverButton(State state, Combat combat, IKokoroApi.IV2.IDroneShiftHookApi.Direction direction)
	{
		var canDoDroneShiftArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.ActionCanDoDroneShiftArgs>();
		var buttonHoveredArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.ActionDroneShiftButtonHoveredArgs>();
		var paymentOptionCanPayForDroneShiftArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionCanPayForDroneShiftArgs>();
		var paymentOptionButtonHoveredArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionDroneShiftButtonHoveredArgs>();
		var preconditionIsDroneShiftAllowedArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PreconditionIsDroneShiftAllowedArgs>();
		var preconditionButtonHoveredArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PreconditionDroneShiftButtonHoveredArgs>();
		var postconditionIsDroneShiftAllowedArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PostconditionIsDroneShiftAllowedArgs>();
		var postconditionButtonHoveredArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PostconditionDroneShiftButtonHoveredArgs>();

		try
		{
			canDoDroneShiftArgs.State = state;
			canDoDroneShiftArgs.Combat = combat;
			canDoDroneShiftArgs.Direction = direction;

			foreach (var entry in FilterEnabled(state, combat, direction, true, Instance.ActionEntries))
			{
				if (!entry.Action.CanDoDroneShiftAction(canDoDroneShiftArgs))
					continue;

				buttonHoveredArgs.State = state;
				buttonHoveredArgs.Combat = combat;
				buttonHoveredArgs.Direction = direction;
				entry.Action.DroneShiftButtonHovered(buttonHoveredArgs);

				paymentOptionCanPayForDroneShiftArgs.State = state;
				paymentOptionCanPayForDroneShiftArgs.Combat = combat;
				paymentOptionCanPayForDroneShiftArgs.Direction = direction;
				paymentOptionCanPayForDroneShiftArgs.Entry = entry;

				foreach (var paymentOption in FilterEnabled(state, combat, direction, entry, true, entry.PaymentOptions))
				{
					if (!paymentOption.CanPayForDroneShift(paymentOptionCanPayForDroneShiftArgs))
						continue;

					paymentOptionButtonHoveredArgs.State = state;
					paymentOptionButtonHoveredArgs.Combat = combat;
					paymentOptionButtonHoveredArgs.Direction = direction;
					paymentOptionButtonHoveredArgs.Entry = entry;
					paymentOption.DroneShiftButtonHovered(paymentOptionButtonHoveredArgs);

					preconditionIsDroneShiftAllowedArgs.State = state;
					preconditionIsDroneShiftAllowedArgs.Combat = combat;
					preconditionIsDroneShiftAllowedArgs.Direction = direction;
					preconditionIsDroneShiftAllowedArgs.Entry = entry;
					preconditionIsDroneShiftAllowedArgs.PaymentOption = paymentOption;
					preconditionIsDroneShiftAllowedArgs.ForRendering = true;

					foreach (var precondition in FilterEnabled(state, combat, direction, entry, true, entry.Preconditions))
					{
						var result = precondition.IsDroneShiftAllowed(preconditionIsDroneShiftAllowedArgs);

						preconditionButtonHoveredArgs.State = state;
						preconditionButtonHoveredArgs.Combat = combat;
						preconditionButtonHoveredArgs.Direction = direction;
						preconditionButtonHoveredArgs.Entry = entry;
						preconditionButtonHoveredArgs.PaymentOption = paymentOption;
						preconditionButtonHoveredArgs.Result = result;
						precondition.DroneShiftButtonHovered(preconditionButtonHoveredArgs);

						if (!result.IsAllowed)
							return;
					}

					postconditionIsDroneShiftAllowedArgs.State = state;
					postconditionIsDroneShiftAllowedArgs.Combat = combat;
					postconditionIsDroneShiftAllowedArgs.Direction = direction;
					postconditionIsDroneShiftAllowedArgs.Entry = entry;
					postconditionIsDroneShiftAllowedArgs.PaymentOption = paymentOption;
					postconditionIsDroneShiftAllowedArgs.ForRendering = true;

					foreach (var postcondition in FilterEnabled(state, combat, direction, entry, paymentOption, true, entry.Postconditions))
					{
						var result = postcondition.IsDroneShiftAllowed(postconditionIsDroneShiftAllowedArgs);

						postconditionButtonHoveredArgs.State = state;
						postconditionButtonHoveredArgs.Combat = combat;
						postconditionButtonHoveredArgs.Direction = direction;
						postconditionButtonHoveredArgs.Entry = entry;
						postconditionButtonHoveredArgs.Result = result;
						postcondition.DroneShiftButtonHovered(postconditionButtonHoveredArgs);

						if (!result.IsAllowed)
							return;
					}

					return;
				}
			}
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(canDoDroneShiftArgs);
			ModEntry.Instance.ArgsPool.Return(buttonHoveredArgs);
			ModEntry.Instance.ArgsPool.Return(paymentOptionCanPayForDroneShiftArgs);
			ModEntry.Instance.ArgsPool.Return(paymentOptionButtonHoveredArgs);
			ModEntry.Instance.ArgsPool.Return(preconditionIsDroneShiftAllowedArgs);
			ModEntry.Instance.ArgsPool.Return(preconditionButtonHoveredArgs);
			ModEntry.Instance.ArgsPool.Return(postconditionIsDroneShiftAllowedArgs);
			ModEntry.Instance.ArgsPool.Return(postconditionButtonHoveredArgs);
		}
	}

	public IKokoroApi.IV2.IDroneShiftHookApi.IRunActionResult RunNextAction(State state, Combat combat, IKokoroApi.IV2.IDroneShiftHookApi.Direction direction)
	{
		var result = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.RunActionResult>();
		var canDoDroneShiftArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.ActionCanDoDroneShiftArgs>();
		var paymentOptionCanPayForDroneShiftArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionCanPayForDroneShiftArgs>();
		var preconditionIsDroneShiftAllowedArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PreconditionIsDroneShiftAllowedArgs>();
		var hookDroneShiftPreconditionFailedArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookDroneShiftPreconditionFailedArgs>();
		var paymentOptionProvideDroneShiftPaymentActionsArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PaymentOptionProvideDroneShiftPaymentActionsArgs>();
		var postconditionIsDroneShiftAllowedArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.PostconditionIsDroneShiftAllowedArgs>();
		var hookDroneShiftPostconditionFailedArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookDroneShiftPostconditionFailedArgs>();
		var actionProvideDroneShiftActionsArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.ActionProvideDroneShiftActionsArgs>();
		var hookAfterDroneShiftArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookAfterDroneShiftArgs>();

		try
		{
			result.State = state;
			result.Combat = combat;
			result.Direction = direction;
			result.Entry = null;
			result.PaymentOption = null;
			result.PreconditionFailed = null;
			result.PostconditionFailed = null;
			result.Success = null;

			if (!combat.PlayerCanAct(state))
				return result;

			foreach (var entry in FilterEnabled(state, combat, direction, false, Instance.ActionEntries))
			{
				canDoDroneShiftArgs.State = state;
				canDoDroneShiftArgs.Combat = combat;
				canDoDroneShiftArgs.Direction = direction;

				if (!entry.Action.CanDoDroneShiftAction(canDoDroneShiftArgs))
					continue;

				foreach (var paymentOption in FilterEnabled(state, combat, direction, entry, false, entry.PaymentOptions))
				{
					paymentOptionCanPayForDroneShiftArgs.State = state;
					paymentOptionCanPayForDroneShiftArgs.Combat = combat;
					paymentOptionCanPayForDroneShiftArgs.Direction = direction;
					paymentOptionCanPayForDroneShiftArgs.Entry = entry;

					if (!paymentOption.CanPayForDroneShift(paymentOptionCanPayForDroneShiftArgs))
						continue;

					result.Entry = entry;
					result.PaymentOption = paymentOption;

					preconditionIsDroneShiftAllowedArgs.State = state;
					preconditionIsDroneShiftAllowedArgs.Combat = combat;
					preconditionIsDroneShiftAllowedArgs.Direction = direction;
					preconditionIsDroneShiftAllowedArgs.Entry = entry;
					preconditionIsDroneShiftAllowedArgs.ForRendering = false;

					foreach (var precondition in FilterEnabled(state, combat, direction, entry, false, entry.Preconditions))
					{
						var preconditionResult = precondition.IsDroneShiftAllowed(preconditionIsDroneShiftAllowedArgs);
						if (!preconditionResult.IsAllowed)
						{
							if (preconditionResult.ShakeShipOnFail)
							{
								Audio.Play(Event.Status_PowerDown);
								state.ship.shake += 1.0;
							}

							combat.Queue(preconditionResult.ActionsOnFail);

							hookDroneShiftPreconditionFailedArgs.State = state;
							hookDroneShiftPreconditionFailedArgs.Combat = combat;
							hookDroneShiftPreconditionFailedArgs.Direction = direction;
							hookDroneShiftPreconditionFailedArgs.Entry = entry;
							hookDroneShiftPreconditionFailedArgs.Precondition = precondition;
							hookDroneShiftPreconditionFailedArgs.QueuedActions = preconditionResult.ActionsOnFail.ToList();

							foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
								hook.DroneShiftPreconditionFailed(hookDroneShiftPreconditionFailedArgs);

							result.PreconditionFailed = hookDroneShiftPreconditionFailedArgs;
							return result;
						}
					}

					paymentOptionProvideDroneShiftPaymentActionsArgs.State = state;
					paymentOptionProvideDroneShiftPaymentActionsArgs.Combat = combat;
					paymentOptionProvideDroneShiftPaymentActionsArgs.Direction = direction;
					paymentOptionProvideDroneShiftPaymentActionsArgs.Entry = entry;

					var paymentActions = paymentOption.ProvideDroneShiftPaymentActions(paymentOptionProvideDroneShiftPaymentActionsArgs);

					postconditionIsDroneShiftAllowedArgs.State = state;
					postconditionIsDroneShiftAllowedArgs.Combat = combat;
					postconditionIsDroneShiftAllowedArgs.Direction = direction;
					postconditionIsDroneShiftAllowedArgs.Entry = entry;
					postconditionIsDroneShiftAllowedArgs.PaymentOption = paymentOption;
					postconditionIsDroneShiftAllowedArgs.ForRendering = false;

					foreach (var postcondition in FilterEnabled(state, combat, direction, entry, paymentOption, false, entry.Postconditions))
					{
						var postconditionResult = postcondition.IsDroneShiftAllowed(postconditionIsDroneShiftAllowedArgs);
						if (!postconditionResult.IsAllowed)
						{
							if (postconditionResult.ShakeShipOnFail)
							{
								Audio.Play(Event.Status_PowerDown);
								state.ship.shake += 1.0;
							}

							List<CardAction> queuedActions = [.. paymentActions, .. postconditionResult.ActionsOnFail];
							combat.Queue(queuedActions);

							hookDroneShiftPostconditionFailedArgs.State = state;
							hookDroneShiftPostconditionFailedArgs.Combat = combat;
							hookDroneShiftPostconditionFailedArgs.Direction = direction;
							hookDroneShiftPostconditionFailedArgs.Entry = entry;
							hookDroneShiftPostconditionFailedArgs.PaymentOption = paymentOption;
							hookDroneShiftPostconditionFailedArgs.Postcondition = postcondition;
							hookDroneShiftPostconditionFailedArgs.QueuedActions = queuedActions;

							foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
								hook.DroneShiftPostconditionFailed(hookDroneShiftPostconditionFailedArgs);

							result.PostconditionFailed = hookDroneShiftPostconditionFailedArgs;
							return result;
						}
					}

					actionProvideDroneShiftActionsArgs.State = state;
					actionProvideDroneShiftActionsArgs.Combat = combat;
					actionProvideDroneShiftActionsArgs.Direction = direction;
					actionProvideDroneShiftActionsArgs.PaymentOption = paymentOption;

					var droneShiftActions = entry.Action.ProvideDroneShiftActions(actionProvideDroneShiftActionsArgs);

					List<CardAction> allActions = [.. paymentActions, .. droneShiftActions];
					combat.Queue(allActions);

					hookAfterDroneShiftArgs.State = state;
					hookAfterDroneShiftArgs.Combat = combat;
					hookAfterDroneShiftArgs.Direction = direction;
					hookAfterDroneShiftArgs.Entry = entry;
					hookAfterDroneShiftArgs.PaymentOption = paymentOption;
					hookAfterDroneShiftArgs.QueuedActions = allActions;

					foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
						hook.AfterDroneShift(hookAfterDroneShiftArgs);

					result.Success = hookAfterDroneShiftArgs;
					return result;
				}
			}
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(result);
			ModEntry.Instance.ArgsPool.Return(canDoDroneShiftArgs);
			ModEntry.Instance.ArgsPool.Return(paymentOptionCanPayForDroneShiftArgs);
			ModEntry.Instance.ArgsPool.Return(preconditionIsDroneShiftAllowedArgs);
			ModEntry.Instance.ArgsPool.Return(hookDroneShiftPreconditionFailedArgs);
			ModEntry.Instance.ArgsPool.Return(paymentOptionProvideDroneShiftPaymentActionsArgs);
			ModEntry.Instance.ArgsPool.Return(postconditionIsDroneShiftAllowedArgs);
			ModEntry.Instance.ArgsPool.Return(hookDroneShiftPostconditionFailedArgs);
			ModEntry.Instance.ArgsPool.Return(actionProvideDroneShiftActionsArgs);
			ModEntry.Instance.ArgsPool.Return(hookAfterDroneShiftArgs);
		}

		return result;
	}

	private static void Combat_RenderDroneShiftButtons_Postfix(Combat __instance, G g)
	{
		if (__instance.isHoveringDroneMove != 2)
			return;
		
		__instance.isHoveringDroneMove = 0;

		IKokoroApi.IV2.IDroneShiftHookApi.Direction typedDirection;
		if (g.hoverKey?.k == StableUK.btn_moveDrones_left)
			typedDirection = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Left;
		else if (g.hoverKey?.k == StableUK.btn_moveDrones_right)
			typedDirection = IKokoroApi.IV2.IDroneShiftHookApi.Direction.Right;
		else
			return;
			
		Instance.DidHoverButton(g.state, __instance, typedDirection);
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
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, new ElementMatch<CodeInstruction>("branch target", i => i.labels.Contains(branchTarget)))
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
					new CodeInstruction(OpCodes.Ldarg_0),
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
					new CodeInstruction(OpCodes.Ldarg_0),
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

	private static bool Combat_RenderDroneShiftButtons_Transpiler_ShouldRender(G g, Combat combat, int direction)
	{
		var shouldShowDroneShiftButtonArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.DroneShiftHookApi.HookShouldShowDroneShiftButtonArgs>();
		try
		{
			shouldShowDroneShiftButtonArgs.State = g.state;
			shouldShowDroneShiftButtonArgs.Combat = combat;
			shouldShowDroneShiftButtonArgs.Direction = (IKokoroApi.IV2.IDroneShiftHookApi.Direction)direction;

			foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, g.state.EnumerateAllArtifacts()))
			{
				var result = hook.ShouldShowDroneShiftButton(shouldShowDroneShiftButtonArgs);
				if (result == true)
					break;
				if (result == false)
					return false;
			}

			return Instance.GetNextAction(g.state, combat, (IKokoroApi.IV2.IDroneShiftHookApi.Direction)direction, true) is not null;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(shouldShowDroneShiftButtonArgs);
		}
	}
	
	private static bool Combat_DoDroneShift_Prefix(Combat __instance, G g, int dir)
	{
		var typedDirection = (IKokoroApi.IV2.IDroneShiftHookApi.Direction)dir;
		Instance.RunNextAction(g.state, __instance, typedDirection);
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

	public void DroneShiftButtonHovered(IKokoroApi.IV2.IDroneShiftHookApi.IDroneShiftPaymentOption.IDroneShiftButtonHoveredArgs args)
		=> args.State.ship.statusEffectPulses[Status.droneShift] = 0.05;
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