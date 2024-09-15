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
	partial class ActionApiImplementation
	{
		public CardAction MakeHidden(CardAction action, bool showTooltips = false)
			=> new AHidden { Action = action, ShowTooltips = showTooltips };
	}
}

internal sealed class HiddenActionManager : IWrappedActionHook
{
	internal static readonly HiddenActionManager Instance = new();
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Transpiler)), priority: Priority.First)
		);
	}

	internal static void SetupLate()
		=> WrappedActionManager.Instance.Register(Instance, 0);
	
	public List<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AHidden hidden)
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
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
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

public sealed class AHidden : CardAction
{
	public CardAction? Action;
	public bool ShowTooltips;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Action is null)
			return;
		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> (!ShowTooltips || Action?.omitFromTooltips == true) ? [] : (Action?.GetTooltips(s) ?? []);

	public override bool CanSkipTimerIfLastEvent()
		=> Action?.CanSkipTimerIfLastEvent() ?? base.CanSkipTimerIfLastEvent();
}