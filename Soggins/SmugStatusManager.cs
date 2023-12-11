using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Soggins;

internal class SmugStatusManager : HookManager<ISmugHook>, ISmugHook
{
	private enum SmugResult
	{
		Botch, Normal, Double
	}

	private static ModEntry Instance => ModEntry.Instance;

	private static readonly Dictionary<Type, int> TimesApologyWasGiven = new();

	private static bool IsDuringTryPlayCard = false;
	private static bool HasPlayNoMatterWhatForFreeSet = false;

	internal SmugStatusManager() : base()
	{
		Register(this, 0);
	}

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(typeof(SmugStatusManager), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(typeof(SmugStatusManager), nameof(Combat_TryPlayCard_Finalizer)),
			transpiler: new HarmonyMethod(typeof(SmugStatusManager), nameof(Combat_TryPlayCard_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(typeof(SmugStatusManager), nameof(Ship_Set_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), "GetStatusSize"),
			postfix: new HarmonyMethod(typeof(SmugStatusManager), nameof(Ship_GetStatusSize_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), "RenderStatusRow"),
			transpiler: new HarmonyMethod(typeof(SmugStatusManager), nameof(Ship_RenderStatusRow_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Make)),
			postfix: new HarmonyMethod(typeof(SmugStatusManager), nameof(Combat_Make_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SmugStatusManager), nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last)
		);
	}

	public void OnCardBotchedBySmug(State state, Combat combat, Card card)
	{
		int extraApologies = state.ship.Get((Status)Instance.ExtraApologiesStatus.Id!.Value);
		for (int i = 0; i < extraApologies; i++)
			combat.Queue(new AAddCard
			{
				card = GenerateAndTrackApology(state, combat, state.rngActions),
				destination = CardDestination.Hand,
				statusPulse = (Status)Instance.ExtraApologiesStatus.Id!.Value
			});
	}

	private static SmugResult GetSmugResult(Ship ship, Rand rng)
	{
		double botchChance = Instance.Api.GetSmugBotchChance(ship);
		double doubleChance = Instance.Api.GetSmugDoubleChance(ship);

		var result = rng.Next();
		if (result < botchChance)
			return SmugResult.Botch;
		else if (result < botchChance + doubleChance)
			return SmugResult.Double;
		else
			return SmugResult.Normal;
	}

	public static Card GenerateAndTrackApology(State state, Combat combat, Rand rng)
	{
		ApologyCard apology;
		WeightedRandom<ApologyCard> weightedRandom = new();
		foreach (var apologyType in ModEntry.ApologyCards)
		{
			apology = (ApologyCard)Activator.CreateInstance(apologyType)!;
			var weight = apology.GetApologyWeight(state, combat, TimesApologyWasGiven.GetValueOrDefault(apologyType));
			if (weight > 0)
				weightedRandom.Add(new(weight, apology));
		}

		apology = weightedRandom.Next(rng);
		TimesApologyWasGiven[apology.GetType()] = TimesApologyWasGiven.GetValueOrDefault(apology.GetType()) + 1;
		return apology;
	}

	private static void Combat_TryPlayCard_Prefix(bool playNoMatterWhatForFree)
	{
		IsDuringTryPlayCard = true;
		HasPlayNoMatterWhatForFreeSet = playNoMatterWhatForFree;
	}

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;

	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldfld("exhaust"),
					ILMatches.Ldarg(4),
					ILMatches.Instruction(OpCodes.Or),
					ILMatches.Stloc<bool>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.CreateLdlocaInstruction(out var ldlocaExhaust)
				.Find(
					ILMatches.Ldloc<List<CardAction>>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Call("Queue")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					ldlocaExhaust,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SmugStatusManager), nameof(Combat_TryPlayCard_Transpiler_ModifyActions)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static List<CardAction> Combat_TryPlayCard_Transpiler_ModifyActions(List<CardAction> actions, State state, Combat combat, Card card, bool playNoMatterWhatForFree, ref bool exhaust)
	{
		if (playNoMatterWhatForFree)
			return actions;

		var handlingHook = Instance.FrogproofManager.GetHandlingHook(state, combat, card, FrogproofHookContext.Action);
		if (handlingHook is not null)
		{
			handlingHook.PayForFrogproof(state, combat, card);
			return actions;
		}

		var result = GetSmugResult(state.ship, state.rngActions);
		var swing = Math.Max(card.GetCurrentCost(state), 1);
		switch (result)
		{
			case SmugResult.Botch:
				exhaust = false;
				state.ship.Add((Status)Instance.BotchesStatus.Id!.Value);
				state.ship.PulseStatus((Status)Instance.SmugStatus.Id!.Value);

				actions.Clear();
				for (int i = 0; i < swing; i++)
				{
					actions.Add(new AAddCard
					{
						card = GenerateAndTrackApology(state, combat, state.rngActions),
						destination = CardDestination.Hand
					});
				}

				if (Instance.Api.IsOversmug(state.ship))
					Instance.Api.SetSmug(state.ship, Instance.Api.GetMinSmug(state.ship));
				else
					Instance.Api.AddSmug(state.ship, -swing);

				foreach (var hook in Instance.SmugStatusManager)
					hook.OnCardBotchedBySmug(state, combat, card);
				break;
			case SmugResult.Double:
				state.ship.PulseStatus((Status)Instance.SmugStatus.Id!.Value);

				var toAdd = actions.Select(a => Mutil.DeepCopy(a)).ToList();
				if (actions.Any(a => a is ASpawn))
					toAdd.Insert(0, new ADroneMove { dir = 1 });
				actions.AddRange(toAdd);

				Instance.Api.AddSmug(state.ship, swing);

				foreach (var hook in Instance.SmugStatusManager)
					hook.OnCardDoubledBySmug(state, combat, card);
				break;
		}
		return actions;
	}

	private static void Ship_Set_Prefix(Ship __instance, Status status, ref int n)
	{
		if (status != (Status)Instance.SmugStatus.Id!.Value)
			return;
		n = n <= 0 ? 0 : Math.Clamp(n, 100 + Instance.Api.GetMinSmug(__instance), 100 + Instance.Api.GetMaxSmug(__instance) + 1);
	}

	private static void Ship_GetStatusSize_Postfix(Ship __instance, Status status, ref object __result)
	{
		if (status != (Status)Instance.SmugStatus.Id!.Value)
			return;

		var boxes = Instance.Api.GetMaxSmug(__instance) - Instance.Api.GetMinSmug(__instance) + 1;

		// TODO: use a publicizer, or emit some IL to do this instead. performance must suck
		var statusPlanType = AccessTools.Inner(typeof(Ship), "StatusPlan");
		var asTextField = AccessTools.DeclaredField(statusPlanType, "asText");
		var asBarsField = AccessTools.DeclaredField(statusPlanType, "asBars");
		var barMaxField = AccessTools.DeclaredField(statusPlanType, "barMax");
		var boxWidthField = AccessTools.DeclaredField(statusPlanType, "boxWidth");
		var barTickWidthField = AccessTools.DeclaredField(statusPlanType, "barTickWidth");

		asTextField.SetValue(__result, false);
		asBarsField.SetValue(__result, true);
		barMaxField.SetValue(__result, boxes);
		boxWidthField.SetValue(__result, 17 + boxes * ((int)barTickWidthField.GetValue(__result)! + 1));
	}

	private static IEnumerable<CodeInstruction> Ship_RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<KeyValuePair<Status, int>>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.CreateLdlocInstruction(out var ldlocStatusAndAmount)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("barMax"),
					ILMatches.Blt
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocBarIndex)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.Before,
					ILMatches.Ldloca<Color>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Instruction(OpCodes.Ldc_R8),
					ILMatches.Call("fadeAlpha"),
					ILMatches.Br,
					ILMatches.Ldloc<Color>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					ldlocStatusAndAmount,
					ldlocBarIndex,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SmugStatusManager), nameof(Ship_RenderStatusRow_Transpiler_ModifyColor)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Color Ship_RenderStatusRow_Transpiler_ModifyColor(Color color, Ship ship, KeyValuePair<Status, int> statusAndAmount, int barIndex)
	{
		if (statusAndAmount.Key != (Status)Instance.SmugStatus.Id!.Value)
			return color;
		if (Instance.Api.IsOversmug(ship))
			return Colors.downside;

		int smugIndex = barIndex + Instance.Api.GetMinSmug(ship);
		if (smugIndex == 0)
			return Colors.white;

		int smug = Instance.Api.GetSmug(ship) ?? 0;
		if (smug < 0 && smugIndex >= smug && smugIndex < 0)
			return Colors.downside;
		else if (smug > 0 && smugIndex <= smug && smugIndex > 0)
			return Colors.cheevoGold;
		else
			return new Color("b2f2ff").fadeAlpha(0.3);
	}

	private static void Combat_Make_Postfix()
		=> TimesApologyWasGiven.Clear();

	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;

		int constantApologies = s.ship.Get((Status)Instance.ConstantApologiesStatus.Id!.Value);
		for (int i = 0; i < constantApologies; i++)
			c.Queue(new AAddCard
			{
				card = GenerateAndTrackApology(s, c, s.rngActions),
				destination = CardDestination.Hand,
				statusPulse = (Status)Instance.ConstantApologiesStatus.Id!.Value
			});
	}
}
