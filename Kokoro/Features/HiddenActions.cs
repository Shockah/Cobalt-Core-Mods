using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		public bool TryGetHiddenAction(CardAction maybeSpontanenousAction, [MaybeNullWhen(false)] out CardAction action)
		{
			action = maybeSpontanenousAction is AHidden hiddenAction ? hiddenAction.Action : null;
			return action is not null;
		}
		
		public CardAction MakeHidden(CardAction action, bool showTooltips = false)
			=> new AHidden { Action = action, ShowTooltips = showTooltips };
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IHiddenActionsApi HiddenActions { get; } = new HiddenActionsApi();
		
		public sealed class HiddenActionsApi : IKokoroApi.IV2.IHiddenActionsApi
		{
			public IKokoroApi.IV2.IHiddenActionsApi.IHiddenAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.IHiddenActionsApi.IHiddenAction;

			public IKokoroApi.IV2.IHiddenActionsApi.IHiddenAction MakeAction(CardAction action)
				=> new AHidden { Action = action };
		}
	}
}

internal sealed class HiddenActionManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly HiddenActionManager Instance = new();
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Transpiler)), priority: Priority.First)
		);
	}
	
	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
	{
		if (args.Action is not AHidden hidden)
			return null;
		if (hidden.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}
	
	private static IEnumerable<CodeInstruction> Card_MakeAllActionIcons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("GetActionsOverridden"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Transpiler_ModifyActions)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static List<CardAction> Card_MakeAllActionIcons_Transpiler_ModifyActions(List<CardAction> actions)
	{
		var result = actions.Where(a => a is not AHidden).ToList();
		actions.Clear();
		actions.AddRange(result);
		return actions;
	}
}

public sealed class AHidden : CardAction, IKokoroApi.IV2.IHiddenActionsApi.IHiddenAction
{
	public required CardAction Action { get; set; }
	public bool ShowTooltips { get; set; }

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> (!ShowTooltips || Action?.omitFromTooltips == true) ? [] : (Action?.GetTooltips(s) ?? []);

	public override bool CanSkipTimerIfLastEvent()
		=> Action?.CanSkipTimerIfLastEvent() ?? base.CanSkipTimerIfLastEvent();
	
	public IKokoroApi.IV2.IHiddenActionsApi.IHiddenAction SetAction(CardAction value)
	{
		Action = value;
		return this;
	}
	
	public IKokoroApi.IV2.IHiddenActionsApi.IHiddenAction SetShowTooltips(bool value)
	{
		ShowTooltips = value;
		return this;
	}
}