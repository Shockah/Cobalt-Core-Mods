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
			
			public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition DefaultActionAnchorPrecondition
				=> AnchorEvadePrecondition.Instance;
			
			public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition DefaultActionEngineLockPrecondition
				=> EngineLockEvadePrecondition.Instance;
			
			public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition DefaultActionEngineStallPostcondition
				=> EngineStallEvadePostcondition.Instance;

			public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry RegisterAction(IKokoroApi.IV2.IEvadeHookApi.IEvadeAction action, double priority = 0)
			{
				var entry = new EvadeActionEntry(action);
				EvadeManager.Instance.ActionEntries.Add(entry, priority);
				return entry;
			}

			public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult MakePreconditionResult(bool isAllowed)
				=> new EvadePreconditionResult(isAllowed);

			public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IResult MakePostconditionResult(bool isAllowed)
				=> new EvadePostconditionResult(isAllowed);

			public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry? GetNextAction(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction)
				=> EvadeManager.Instance.GetNextAction(state, combat, direction);

			public void DidHoverButton(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction)
				=> EvadeManager.Instance.DidHoverButton(state, combat, direction);

			public IKokoroApi.IV2.IEvadeHookApi.IRunActionResult RunNextAction(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction)
				=> EvadeManager.Instance.RunNextAction(state, combat, direction);

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
					=> PreconditionsOrderedList.Entries
						.Concat(InheritsPreconditionsFrom.SelectMany(e => ((EvadeActionEntry)e).PreconditionsOrderedList.Entries))
						.OrderByDescending(e => e.OrderingValue)
						.Select(e => e.Element);

				public IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition> Postconditions
					=> PostconditionsOrderedList.Entries
						.Concat(InheritsPreconditionsFrom.SelectMany(e => ((EvadeActionEntry)e).PostconditionsOrderedList.Entries))
						.OrderByDescending(e => e.OrderingValue)
						.Select(e => e.Element);

				private readonly OrderedList<IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption, double> PaymentOptionsOrderedList = new(ascending: false);
				private readonly OrderedList<IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition, double> PreconditionsOrderedList = new(ascending: false);
				private readonly OrderedList<IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition, double> PostconditionsOrderedList = new(ascending: false);
				private readonly List<IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry> InheritsPreconditionsFrom = [];
				private readonly List<IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry> InheritsPostconditionsFrom = [];

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

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry InheritPreconditions(IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry entry)
				{
					if (InheritsPreconditionsFrom.Contains(entry))
						return this;
					InheritsPreconditionsFrom.Add(entry);
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry RegisterPostcondition(IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition postcondition, double priority = 0)
				{
					PostconditionsOrderedList.Add(postcondition, priority);
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry UnregisterPostcondition(IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition postcondition)
				{
					PostconditionsOrderedList.Remove(postcondition);
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry InheritPostconditions(IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry entry)
				{
					if (InheritsPostconditionsFrom.Contains(entry))
						return this;
					InheritsPostconditionsFrom.Add(entry);
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
			
			internal sealed class EvadePostconditionResult(
				bool isAllowed
			) : IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IResult
			{
				public bool IsAllowed { get; set; } = isAllowed;
				public bool ShakeShipOnFail { get; set; } = true;
				public IList<CardAction> ActionsOnFail { get; set; } = [];
				
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IResult SetIsAllowed(bool value)
				{
					IsAllowed = value;
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IResult SetShakeShipOnFail(bool value)
				{
					ShakeShipOnFail = value;
					return this;
				}

				public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IResult SetActionsOnFail(IList<CardAction> value)
				{
					ActionsOnFail = value;
					return this;
				}
			}
			
			internal sealed class RunActionResult : IKokoroApi.IV2.IEvadeHookApi.IRunActionResult
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly RunActionResult Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry? Entry { get; internal set; }
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption? PaymentOption { get; internal set; }
				public IKokoroApi.IV2.IEvadeHookApi.IHook.IEvadePreconditionFailedArgs? PreconditionFailed { get; internal set; }
				public IKokoroApi.IV2.IEvadeHookApi.IHook.IEvadePostconditionFailedArgs? PostconditionFailed { get; internal set; }
				public IKokoroApi.IV2.IEvadeHookApi.IHook.IAfterEvadeArgs? Success { get; internal set; }
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
			
			internal sealed class ActionEvadeButtonHoveredArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadeAction.IEvadeButtonHoveredArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ActionEvadeButtonHoveredArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
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
			
			internal sealed class PaymentOptionEvadeButtonHoveredArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IEvadeButtonHoveredArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PaymentOptionEvadeButtonHoveredArgs Instance = new();
				
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
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class PreconditionEvadeButtonHoveredArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IEvadeButtonHoveredArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PreconditionEvadeButtonHoveredArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult Result { get; internal set; } = null!;
			}
			
			internal sealed class PostconditionIsEvadeAllowedArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IIsEvadeAllowedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PostconditionIsEvadeAllowedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
				public bool ForRendering { get; internal set; }
			}
			
			internal sealed class PostconditionEvadeButtonHoveredArgs : IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IEvadeButtonHoveredArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly PostconditionEvadeButtonHoveredArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IResult Result { get; internal set; } = null!;
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
			
			internal sealed class HookIsEvadePostconditionEnabledArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePostconditionEnabledArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookIsEvadePostconditionEnabledArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition Postcondition { get; internal set; } = null!;
			}
			
			internal sealed class HookEvadePreconditionFailedArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IEvadePreconditionFailedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookEvadePreconditionFailedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition Precondition { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
			}
			
			internal sealed class HookEvadePostconditionFailedArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IEvadePostconditionFailedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookEvadePostconditionFailedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry Entry { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption PaymentOption { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition Postcondition { get; internal set; } = null!;
				public IReadOnlyList<CardAction> QueuedActions { get; internal set; } = null!;
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
			
			internal sealed class HookShouldShowEvadeButtonArgs : IKokoroApi.IV2.IEvadeHookApi.IHook.IShouldShowEvadeButtonArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly HookShouldShowEvadeButtonArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IEvadeHookApi.Direction Direction { get; internal set; } = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
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
			.RegisterPrecondition(AnchorEvadePrecondition.Instance)
			.RegisterPrecondition(EngineLockEvadePrecondition.Instance)
			.RegisterPostcondition(EngineStallEvadePostcondition.Instance);
		DefaultActionEntry = defaultActionEntry;
		ActionEntries.Add(defaultActionEntry, 0);
		
		HookManager.Register(DebugEvadeHook.Instance, 1_000_000_000);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderMoveButtons)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Postfix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DoEvade)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DoEvade_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Transpiler))
		);
	}

	private static IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IEvadeHookApi.Direction direction,
		IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry> enumerable
	)
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

	private static IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IEvadeHookApi.Direction direction,
		IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry entry,
		IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption> enumerable
	)
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

	private static IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IEvadeHookApi.Direction direction,
		IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry entry,
		IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition> enumerable
	)
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

	private static IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition> FilterEnabled(
		State state,
		Combat combat,
		IKokoroApi.IV2.IEvadeHookApi.Direction direction,
		IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry entry,
		IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption paymentOption,
		IEnumerable<IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition> enumerable
	)
	{
		var args = ApiImplementation.V2Api.EvadeHookApi.HookIsEvadePostconditionEnabledArgs.Instance;
		args.State = state;
		args.Combat = combat;
		args.Direction = direction;
		args.Entry = entry;
		args.PaymentOption = paymentOption;
		
		var hooks = Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
		return enumerable.Where(postcondition =>
		{
			args.Postcondition = postcondition;
			return hooks.All(h => h.IsEvadePostconditionEnabled(args));
		});
	}

	public IKokoroApi.IV2.IEvadeHookApi.IEvadeActionEntry? GetNextAction(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction)
	{
		var canDoEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.ActionCanDoEvadeArgs.Instance;
		canDoEvadeArgs.State = state;
		canDoEvadeArgs.Combat = combat;
		canDoEvadeArgs.Direction = direction;

		return FilterEnabled(state, combat, direction, Instance.ActionEntries).FirstOrDefault(entry =>
		{
			if (!entry.Action.CanDoEvadeAction(canDoEvadeArgs))
				return false;
				
			var paymentOptionCanPayForEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionCanPayForEvadeArgs.Instance;
			paymentOptionCanPayForEvadeArgs.State = state;
			paymentOptionCanPayForEvadeArgs.Combat = combat;
			paymentOptionCanPayForEvadeArgs.Direction = direction;
			paymentOptionCanPayForEvadeArgs.Entry = entry;

			if (!FilterEnabled(state, combat, direction, entry, entry.PaymentOptions).Any(paymentOption => paymentOption.CanPayForEvade(paymentOptionCanPayForEvadeArgs)))
				return false;
				
			var preconditionIsEvadeAllowedArgs = ApiImplementation.V2Api.EvadeHookApi.PreconditionIsEvadeAllowedArgs.Instance;
			preconditionIsEvadeAllowedArgs.State = state;
			preconditionIsEvadeAllowedArgs.Combat = combat;
			preconditionIsEvadeAllowedArgs.Direction = direction;
			preconditionIsEvadeAllowedArgs.Entry = entry;
			preconditionIsEvadeAllowedArgs.ForRendering = true;

			if (FilterEnabled(state, combat, direction, entry, entry.Preconditions).Any(precondition => !precondition.IsEvadeAllowed(preconditionIsEvadeAllowedArgs).IsAllowed))
				return false;

			return true;
		});
	}

	public void DidHoverButton(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction)
	{
		var canDoEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.ActionCanDoEvadeArgs.Instance;
		canDoEvadeArgs.State = state;
		canDoEvadeArgs.Combat = combat;
		canDoEvadeArgs.Direction = direction;

		foreach (var entry in FilterEnabled(state, combat, direction, Instance.ActionEntries))
		{
			if (!entry.Action.CanDoEvadeAction(canDoEvadeArgs))
				continue;
			
			var buttonHoveredArgs = ApiImplementation.V2Api.EvadeHookApi.ActionEvadeButtonHoveredArgs.Instance;
			buttonHoveredArgs.State = state;
			buttonHoveredArgs.Combat = combat;
			buttonHoveredArgs.Direction = direction;
			entry.Action.EvadeButtonHovered(buttonHoveredArgs);
			
			var paymentOptionCanPayForEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionCanPayForEvadeArgs.Instance;
			paymentOptionCanPayForEvadeArgs.State = state;
			paymentOptionCanPayForEvadeArgs.Combat = combat;
			paymentOptionCanPayForEvadeArgs.Direction = direction;
			paymentOptionCanPayForEvadeArgs.Entry = entry;
			
			foreach (var paymentOption in FilterEnabled(state, combat, direction, entry, entry.PaymentOptions))
			{
				if (!paymentOption.CanPayForEvade(paymentOptionCanPayForEvadeArgs))
					continue;

				var paymentOptionButtonHoveredArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionEvadeButtonHoveredArgs.Instance;
				paymentOptionButtonHoveredArgs.State = state;
				paymentOptionButtonHoveredArgs.Combat = combat;
				paymentOptionButtonHoveredArgs.Direction = direction;
				paymentOptionButtonHoveredArgs.Entry = entry;
				paymentOption.EvadeButtonHovered(paymentOptionButtonHoveredArgs);
			
				var preconditionIsEvadeAllowedArgs = ApiImplementation.V2Api.EvadeHookApi.PreconditionIsEvadeAllowedArgs.Instance;
				preconditionIsEvadeAllowedArgs.State = state;
				preconditionIsEvadeAllowedArgs.Combat = combat;
				preconditionIsEvadeAllowedArgs.Direction = direction;
				preconditionIsEvadeAllowedArgs.Entry = entry;
				preconditionIsEvadeAllowedArgs.PaymentOption = paymentOption;
				preconditionIsEvadeAllowedArgs.ForRendering = true;

				foreach (var precondition in FilterEnabled(state, combat, direction, entry, entry.Preconditions))
				{
					var result = precondition.IsEvadeAllowed(preconditionIsEvadeAllowedArgs);

					var preconditionButtonHoveredArgs = ApiImplementation.V2Api.EvadeHookApi.PreconditionEvadeButtonHoveredArgs.Instance;
					preconditionButtonHoveredArgs.State = state;
					preconditionButtonHoveredArgs.Combat = combat;
					preconditionButtonHoveredArgs.Direction = direction;
					preconditionButtonHoveredArgs.Entry = entry;
					preconditionButtonHoveredArgs.PaymentOption = paymentOption;
					preconditionButtonHoveredArgs.Result = result;
					precondition.EvadeButtonHovered(preconditionButtonHoveredArgs);

					if (!result.IsAllowed)
						return;
				}
				
				var postconditionIsEvadeAllowedArgs = ApiImplementation.V2Api.EvadeHookApi.PostconditionIsEvadeAllowedArgs.Instance;
				postconditionIsEvadeAllowedArgs.State = state;
				postconditionIsEvadeAllowedArgs.Combat = combat;
				postconditionIsEvadeAllowedArgs.Direction = direction;
				postconditionIsEvadeAllowedArgs.Entry = entry;
				postconditionIsEvadeAllowedArgs.PaymentOption = paymentOption;
				postconditionIsEvadeAllowedArgs.ForRendering = true;

				foreach (var postcondition in FilterEnabled(state, combat, direction, entry, paymentOption, entry.Postconditions))
				{
					var result = postcondition.IsEvadeAllowed(postconditionIsEvadeAllowedArgs);

					var postconditionButtonHoveredArgs = ApiImplementation.V2Api.EvadeHookApi.PostconditionEvadeButtonHoveredArgs.Instance;
					postconditionButtonHoveredArgs.State = state;
					postconditionButtonHoveredArgs.Combat = combat;
					postconditionButtonHoveredArgs.Direction = direction;
					postconditionButtonHoveredArgs.Entry = entry;
					postconditionButtonHoveredArgs.Result = result;
					postcondition.EvadeButtonHovered(postconditionButtonHoveredArgs);

					if (!result.IsAllowed)
						return;
				}

				return;
			}
		}
	}

	public IKokoroApi.IV2.IEvadeHookApi.IRunActionResult RunNextAction(State state, Combat combat, IKokoroApi.IV2.IEvadeHookApi.Direction direction)
	{
		var result = ApiImplementation.V2Api.EvadeHookApi.RunActionResult.Instance;
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

		foreach (var entry in FilterEnabled(state, combat, direction, Instance.ActionEntries))
		{
			var canDoEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.ActionCanDoEvadeArgs.Instance;
			canDoEvadeArgs.State = state;
			canDoEvadeArgs.Combat = combat;
			canDoEvadeArgs.Direction = direction;

			if (!entry.Action.CanDoEvadeAction(canDoEvadeArgs))
				continue;

			foreach (var paymentOption in FilterEnabled(state, combat, direction, entry, entry.PaymentOptions))
			{
				var paymentOptionCanPayForEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionCanPayForEvadeArgs.Instance;
				paymentOptionCanPayForEvadeArgs.State = state;
				paymentOptionCanPayForEvadeArgs.Combat = combat;
				paymentOptionCanPayForEvadeArgs.Direction = direction;
				paymentOptionCanPayForEvadeArgs.Entry = entry;

				if (!paymentOption.CanPayForEvade(paymentOptionCanPayForEvadeArgs))
					continue;

				result.Entry = entry;
				result.PaymentOption = paymentOption;
				
				var preconditionIsEvadeAllowedArgs = ApiImplementation.V2Api.EvadeHookApi.PreconditionIsEvadeAllowedArgs.Instance;
				preconditionIsEvadeAllowedArgs.State = state;
				preconditionIsEvadeAllowedArgs.Combat = combat;
				preconditionIsEvadeAllowedArgs.Direction = direction;
				preconditionIsEvadeAllowedArgs.Entry = entry;
				preconditionIsEvadeAllowedArgs.ForRendering = false;
				
				foreach (var precondition in FilterEnabled(state, combat, direction, entry, entry.Preconditions))
				{
					var preconditionResult = precondition.IsEvadeAllowed(preconditionIsEvadeAllowedArgs);
					if (!preconditionResult.IsAllowed)
					{
						if (preconditionResult.ShakeShipOnFail)
						{
							Audio.Play(Event.Status_PowerDown);
							state.ship.shake += 1.0;
						}
					
						combat.Queue(preconditionResult.ActionsOnFail);
					
						var hookEvadePreconditionFailedArgs = ApiImplementation.V2Api.EvadeHookApi.HookEvadePreconditionFailedArgs.Instance;
						hookEvadePreconditionFailedArgs.State = state;
						hookEvadePreconditionFailedArgs.Combat = combat;
						hookEvadePreconditionFailedArgs.Direction = direction;
						hookEvadePreconditionFailedArgs.Entry = entry;
						hookEvadePreconditionFailedArgs.Precondition = precondition;
						hookEvadePreconditionFailedArgs.QueuedActions = preconditionResult.ActionsOnFail.ToList();
				
						foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
							hook.EvadePreconditionFailed(hookEvadePreconditionFailedArgs);

						result.PreconditionFailed = hookEvadePreconditionFailedArgs;
						return result;
					}
				}
				
				var paymentOptionProvideEvadePaymentActionsArgs = ApiImplementation.V2Api.EvadeHookApi.PaymentOptionProvideEvadePaymentActionsArgs.Instance;
				paymentOptionProvideEvadePaymentActionsArgs.State = state;
				paymentOptionProvideEvadePaymentActionsArgs.Combat = combat;
				paymentOptionProvideEvadePaymentActionsArgs.Direction = direction;
				paymentOptionProvideEvadePaymentActionsArgs.Entry = entry;

				var paymentActions = paymentOption.ProvideEvadePaymentActions(paymentOptionProvideEvadePaymentActionsArgs);
				
				var postconditionIsEvadeAllowedArgs = ApiImplementation.V2Api.EvadeHookApi.PostconditionIsEvadeAllowedArgs.Instance;
				postconditionIsEvadeAllowedArgs.State = state;
				postconditionIsEvadeAllowedArgs.Combat = combat;
				postconditionIsEvadeAllowedArgs.Direction = direction;
				postconditionIsEvadeAllowedArgs.Entry = entry;
				postconditionIsEvadeAllowedArgs.PaymentOption = paymentOption;
				postconditionIsEvadeAllowedArgs.ForRendering = false;
				
				foreach (var postcondition in FilterEnabled(state, combat, direction, entry, paymentOption, entry.Postconditions))
				{
					var postconditionResult = postcondition.IsEvadeAllowed(postconditionIsEvadeAllowedArgs);
					if (!postconditionResult.IsAllowed)
					{
						if (postconditionResult.ShakeShipOnFail)
						{
							Audio.Play(Event.Status_PowerDown);
							state.ship.shake += 1.0;
						}

						List<CardAction> queuedActions = [.. paymentActions, .. postconditionResult.ActionsOnFail];
						combat.Queue(queuedActions);
						
						var hookEvadePostconditionFailedArgs = ApiImplementation.V2Api.EvadeHookApi.HookEvadePostconditionFailedArgs.Instance;
						hookEvadePostconditionFailedArgs.State = state;
						hookEvadePostconditionFailedArgs.Combat = combat;
						hookEvadePostconditionFailedArgs.Direction = direction;
						hookEvadePostconditionFailedArgs.Entry = entry;
						hookEvadePostconditionFailedArgs.PaymentOption = paymentOption;
						hookEvadePostconditionFailedArgs.Postcondition = postcondition;
						hookEvadePostconditionFailedArgs.QueuedActions = queuedActions;
				
						foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
							hook.EvadePostconditionFailed(hookEvadePostconditionFailedArgs);

						result.PostconditionFailed = hookEvadePostconditionFailedArgs;
						return result;
					}
				}
				
				var actionProvideEvadeActionsArgs = ApiImplementation.V2Api.EvadeHookApi.ActionProvideEvadeActionsArgs.Instance;
				actionProvideEvadeActionsArgs.State = state;
				actionProvideEvadeActionsArgs.Combat = combat;
				actionProvideEvadeActionsArgs.Direction = direction;
				actionProvideEvadeActionsArgs.PaymentOption = paymentOption;

				var evadeActions = entry.Action.ProvideEvadeActions(actionProvideEvadeActionsArgs);

				List<CardAction> allActions = [.. paymentActions, .. evadeActions];
				combat.Queue(allActions);
				
				var hookAfterEvadeArgs = ApiImplementation.V2Api.EvadeHookApi.HookAfterEvadeArgs.Instance;
				hookAfterEvadeArgs.State = state;
				hookAfterEvadeArgs.Combat = combat;
				hookAfterEvadeArgs.Direction = direction;
				hookAfterEvadeArgs.Entry = entry;
				hookAfterEvadeArgs.PaymentOption = paymentOption;
				hookAfterEvadeArgs.QueuedActions = allActions;
				
				foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
					hook.AfterEvade(hookAfterEvadeArgs);

				result.Success = hookAfterEvadeArgs;
				return result;
			}
		}

		return result;
	}

	private static void Combat_RenderMoveButtons_Postfix(Combat __instance, G g)
	{
		if (__instance.isHoveringMove != 2)
			return;
		
		__instance.isHoveringMove = 0;

		IKokoroApi.IV2.IEvadeHookApi.Direction typedDirection;
		if (g.hoverKey?.k == StableUK.btn_move_left)
			typedDirection = IKokoroApi.IV2.IEvadeHookApi.Direction.Left;
		else if (g.hoverKey?.k == StableUK.btn_move_right)
			typedDirection = IKokoroApi.IV2.IEvadeHookApi.Direction.Right;
		else
			return;
		
		Instance.DidHoverButton(g.state, __instance, typedDirection);
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
				.Find([
					ILMatches.Ldarg(1).ExtractLabels(out labels),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.lockdown),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.Replace(new CodeInstruction(OpCodes.Nop).WithLabels(labels))
				.Find([
					ILMatches.Isinst<TrashAnchor>(),
					ILMatches.Brfalse,
					ILMatches.Instruction(OpCodes.Leave)
				])
				.Replace(new CodeInstruction(OpCodes.Pop))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldarg(1).Anchor(out var gPointer1),
					ILMatches.LdcI4((int)StableUK.btn_move_left),
					ILMatches.AnyCall,
					ILMatches.Stloc<UIKey>(originalMethod)
				])
				.Anchors()
				.PointerMatcher(gPointer1)
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
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
					new CodeInstruction(OpCodes.Ldarg_0),
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

	private static bool Combat_RenderMoveButtons_Transpiler_ShouldRender(G g, Combat combat, int direction)
	{
		var shouldShowEvadeButtonArgs = ApiImplementation.V2Api.EvadeHookApi.HookShouldShowEvadeButtonArgs.Instance;
		shouldShowEvadeButtonArgs.State = g.state;
		shouldShowEvadeButtonArgs.Combat = combat;
		shouldShowEvadeButtonArgs.Direction = (IKokoroApi.IV2.IEvadeHookApi.Direction)direction;

		foreach (var hook in Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, g.state.EnumerateAllArtifacts()))
		{
			var result = hook.ShouldShowEvadeButton(shouldShowEvadeButtonArgs);
			if (result == true)
				break;
			if (result == false)
				return false;
		}

		return Instance.GetNextAction(g.state, combat, (IKokoroApi.IV2.IEvadeHookApi.Direction)direction) is not null;
	}
	
	private static bool Combat_DoEvade_Prefix(Combat __instance, G g, int dir)
	{
		var typedDirection = (IKokoroApi.IV2.IEvadeHookApi.Direction)dir;
		Instance.RunNextAction(g.state, __instance, typedDirection);
		return false;
	}

	private static IEnumerable<CodeInstruction> AMove_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(3),
					ILMatches.Ldfld("hand"),
					ILMatches.Call("OfType"),
					ILMatches.Call("Any"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(AMove), nameof(AMove.fromEvade))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				])
				.Find([
					ILMatches.Ldloc<Ship>(originalMethod),
					ILMatches.LdcI4((int)Status.lockdown),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble.GetBranchTarget(out branchTarget)
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(AMove), nameof(AMove.fromEvade))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				])
				.Find([
					ILMatches.Ldloc<Ship>(originalMethod),
					ILMatches.LdcI4((int)Status.engineStall),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble.GetBranchTarget(out branchTarget)
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(AMove), nameof(AMove.fromEvade))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
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

	public void EvadeButtonHovered(IKokoroApi.IV2.IEvadeHookApi.IEvadePaymentOption.IEvadeButtonHoveredArgs args)
		=> args.State.ship.statusEffectPulses[Status.evade] = 0.05;
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

internal sealed class EngineLockEvadePrecondition : IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition
{
	public static EngineLockEvadePrecondition Instance { get; private set; } = new();
	
	private EngineLockEvadePrecondition() { }
	
	public IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IResult IsEvadeAllowed(IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IIsEvadeAllowedArgs args)
	{
		var isLocked = args.State.ship.Get(Status.lockdown) > 0;
		return ModEntry.Instance.Api.V2.EvadeHook.MakePreconditionResult(!isLocked);
	}

	public void EvadeButtonHovered(IKokoroApi.IV2.IEvadeHookApi.IEvadePrecondition.IEvadeButtonHoveredArgs args)
		=> args.State.ship.statusEffectPulses[Status.lockdown] = 0.05;
}

internal sealed class EngineStallEvadePostcondition : IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition
{
	public static EngineStallEvadePostcondition Instance { get; private set; } = new();
	
	private EngineStallEvadePostcondition() { }
	
	public IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IResult IsEvadeAllowed(IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IIsEvadeAllowedArgs args)
	{
		var isLocked = args.State.ship.Get(Status.engineStall) > 0;
		if (isLocked && !args.ForRendering)
			args.State.ship.Add(Status.engineStall, -1);
		return ModEntry.Instance.Api.V2.EvadeHook.MakePostconditionResult(!isLocked);
	}

	public void EvadeButtonHovered(IKokoroApi.IV2.IEvadeHookApi.IEvadePostcondition.IEvadeButtonHoveredArgs args)
		=> args.State.ship.statusEffectPulses[Status.engineStall] = 0.05;
}

internal sealed class DebugEvadeHook : IKokoroApi.IV2.IEvadeHookApi.IHook
{
	public static DebugEvadeHook Instance { get; private set; } = new();
	
	private DebugEvadeHook() { }
	
	public bool IsEvadePreconditionEnabled(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePreconditionEnabledArgs args)
	{
		if (args.Entry != ModEntry.Instance.Api.V2.EvadeHook.DefaultAction)
			return true;
		if (!FeatureFlags.Debug || !Input.shift)
			return true;
		return false;
	}
	
	public bool IsEvadePostconditionEnabled(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePostconditionEnabledArgs args)
	{
		if (args.Entry != ModEntry.Instance.Api.V2.EvadeHook.DefaultAction)
			return true;
		if (!FeatureFlags.Debug || !Input.shift)
			return true;
		return false;
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