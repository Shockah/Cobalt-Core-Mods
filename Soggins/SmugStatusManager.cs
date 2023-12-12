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

internal class SmugStatusManager : HookManager<ISmugHook>, ISmugHook, IStatusRenderHook
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
		Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
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
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Make)),
			postfix: new HarmonyMethod(typeof(SmugStatusManager), nameof(Combat_Make_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SmugStatusManager), nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SmugStatusManager), nameof(Card_GetAllTooltips_Postfix)), Priority.Normal - 1)
		);
	}

	public int ModifyApologyAmountForBotchingBySmug(State state, Combat combat, Card card, int amount)
	{
		int extraApologies = state.ship.Get((Status)Instance.ExtraApologiesStatus.Id!.Value);
		return Math.Max(amount + extraApologies, 1);
	}

	public IEnumerable<Status> GetExtraStatusesToShow(State state, Combat combat, Ship ship)
	{
		if (Instance.Api.GetSmug(ship) is not null)
			yield return (Status)Instance.SmugStatus.Id!.Value;
	}

	public bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount)
		=> status == (Status)Instance.SmuggedStatus.Id!.Value ? false : null;

	public bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount)
		=> status == (Status)Instance.SmugStatus.Id!.Value ? true : null;

	public (IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount)
	{
		var barCount = Instance.Api.GetMaxSmug(ship) - Instance.Api.GetMinSmug(ship) + 1;
		var colors = new Color[barCount];

		if (Instance.Api.IsOversmug(ship))
		{
			Array.Fill(colors, Colors.downside);
			return (colors, null);
		}

		for (int barIndex = 0; barIndex < colors.Length; barIndex++)
		{
			var smugIndex = barIndex + Instance.Api.GetMinSmug(ship);
			if (smugIndex == 0)
			{
				colors[barIndex] = Colors.white;
				continue;
			}

			var smug = Instance.Api.GetSmug(ship) ?? 0;
			if (smug < 0 && smugIndex >= smug && smugIndex < 0)
				colors[barIndex] = Colors.downside;
			else if (smug > 0 && smugIndex <= smug && smugIndex > 0)
				colors[barIndex] = Colors.cheevoGold;
			else
				colors[barIndex] = Instance.KokoroApi.DefaultInactiveStatusBarColor;
		}
		return (colors, null);
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
		apology.ApologyFlavorText = $"<c=B79CE5>{string.Format(I18n.ApologyFlavorTexts[rng.NextInt() % I18n.ApologyFlavorTexts.Length], TimesApologyWasGiven.Values.Sum())}</c>";
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

				actions.Clear();
				actions.Add(new AStatus
				{
					status = (Status)Instance.BotchesStatus.Id!.Value,
					statusAmount = 1,
					targetPlayer = true
				});

				bool isOversmug = Instance.Api.IsOversmug(state.ship);
				actions.Add(new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					mode = isOversmug ? AStatusMode.Set : AStatusMode.Add,
					statusAmount = isOversmug ? Instance.Api.GetMinSmug(state.ship) : -swing,
					targetPlayer = true
				});

				int apologies = swing;
				foreach (var hook in Instance.SmugStatusManager)
					apologies = hook.ModifyApologyAmountForBotchingBySmug(state, combat, card, apologies);
				
				for (int i = 0; i < apologies; i++)
				{
					actions.Add(new AAddCard
					{
						card = GenerateAndTrackApology(state, combat, state.rngActions),
						destination = CardDestination.Hand
					});
				}

				foreach (var hook in Instance.SmugStatusManager)
					hook.OnCardBotchedBySmug(state, combat, card);
				break;
			case SmugResult.Double:
				var toAdd = card.GetActionsOverridden(state, combat)
					.Where(a => a is not AEndTurn)
					.ToList();
				if (actions.Any(a => a is ASpawn))
					toAdd.Add(new ADroneMove { dir = 1 });
				toAdd.Insert(0, new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					statusAmount = swing,
					targetPlayer = true
				});
				actions.InsertRange(0, toAdd);

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
		n = n <= 0 ? 0 : Math.Clamp(n, Instance.Api.GetMinSmug(__instance), Instance.Api.GetMaxSmug(__instance) + 1);
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

	private static void Card_GetAllTooltips_Postfix(Card __instance, G g, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (__instance is not ApologyCard apology)
			return;
		if (string.IsNullOrEmpty(apology.ApologyFlavorText))
			return;
		var tooltipToYield = new TTText(apology.ApologyFlavorText);

		IEnumerable<Tooltip> ModifyTooltips(IEnumerable<Tooltip> tooltips)
		{
			bool yieldedFlavorText = false;

			foreach (var tooltip in tooltips)
			{
				if (!yieldedFlavorText && tooltip is TTGlossary glossary && glossary.key.StartsWith("cardtrait.") && glossary.key != "cardtrait.unplayable")
				{
					yield return tooltipToYield;
					yieldedFlavorText = true;
				}
				yield return tooltip;
			}

			if (!yieldedFlavorText)
				yield return tooltipToYield;
		}

		__result = ModifyTooltips(__result);
	}
}
