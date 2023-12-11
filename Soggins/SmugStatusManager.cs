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

internal static class SmugStatusManager
{
	private enum SmugResult
	{
		Botch, Normal, Double
	}

	private static ModEntry Instance => ModEntry.Instance;

	private static bool IsDuringTryPlayCard = false;
	private static bool HasPlayNoMatterWhatForFreeSet = false;

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(typeof(SmugStatusManager), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(typeof(SmugStatusManager), nameof(Combat_TryPlayCard_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(typeof(SmugStatusManager), nameof(Card_GetActionsOverridden_Postfix))
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

	private static void Combat_TryPlayCard_Prefix(bool playNoMatterWhatForFree)
	{
		IsDuringTryPlayCard = true;
		HasPlayNoMatterWhatForFreeSet = playNoMatterWhatForFree;
	}

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;

	private static void Card_GetActionsOverridden_Postfix(Card __instance, State s, ref List<CardAction> __result)
	{
		if (!IsDuringTryPlayCard)
			return;
		if (HasPlayNoMatterWhatForFreeSet)
			return;

		var hook = Instance.FrogproofManager.GetHandlingHook(s, s.route as Combat, __instance, FrogproofHookContext.Action);
		if (hook is not null)
		{
			hook.PayForFrogproof(s, s.route as Combat, __instance);
			return;
		}

		var result = GetSmugResult(s.ship, s.rngActions);
		switch (result)
		{
			case SmugResult.Botch:
				s.ship.PulseStatus((Status)Instance.SmugStatus.Id!.Value);
				__result.Clear();
				for (int i = 0; i < __instance.GetCurrentCost(s); i++)
					__result.Add(new AAddCard
					{
						card = (Card)Activator.CreateInstance(ModEntry.ApologyCards[s.rngActions.NextInt() % ModEntry.ApologyCards.Length])!,
						destination = CardDestination.Hand
					});

				if (Instance.Api.IsOversmug(s.ship))
					Instance.Api.SetSmug(s.ship, Instance.Api.GetMinSmug(s.ship));
				else
					Instance.Api.AddSmug(s.ship, -1);
				break;
			case SmugResult.Double:
				s.ship.PulseStatus((Status)Instance.SmugStatus.Id!.Value);
				var toAdd = __result.Select(a => Mutil.DeepCopy(a)).ToList();
				if (__result.Any(a => a is ASpawn))
					toAdd.Insert(0, new ADroneMove { dir = 1 });
				__result.AddRange(toAdd);
				Instance.Api.AddSmug(s.ship, 1);
				break;
		}
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
}
