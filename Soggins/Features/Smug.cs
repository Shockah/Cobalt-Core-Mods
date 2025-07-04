﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Soggins;

internal sealed class SmugStatusManager : HookManager<ISmugHook>
{
	private sealed class ExtraApologiesSmugHook : ISmugHook
	{
		public int ModifyApologyAmountForBotchingBySmug(State state, Combat combat, Card card, int amount)
		{
			var extraApologies = state.ship.Get((Status)Instance.ExtraApologiesStatus.Id!.Value);
			return Math.Max(amount + extraApologies, 1);
		}
	}

	private sealed class DoublersLuckSmugHook : ISmugHook
	{
		public double ModifySmugDoubleChance(State state, Ship ship, Card? card, double chance)
			=> chance * (ship.Get((Status)Instance.DoublersLuckStatus.Id!.Value) + 1);
	}

	private sealed class SmugClampStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
		{
			if (args.Status != (Status)Instance.SmugStatus.Id!.Value)
				return args.NewAmount;
			return Math.Clamp(args.NewAmount, Instance.Api.GetMinSmug(args.Ship), Instance.Api.GetMaxSmug(args.Ship) + 1);
		}
	}

	private static ModEntry Instance => ModEntry.Instance;

	internal SmugStatusManager() : base(Instance.Package.Manifest.UniqueName)
	{
		Register(new ExtraApologiesSmugHook(), 0);
		Register(new DoublersLuckSmugHook(), -100);
		Instance.KokoroApi.StatusLogic.RegisterHook(new SmugClampStatusLogicHook(), double.MinValue);
		
		// bump Limited stacks up after botching a card
		Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state) =>
		{
			if (Instance.Helper.ModData.TryGetModData<int>(card, "LimitedUsesToRestoreAfterBotching", out var limitedUsesToRestoreAfterBotching))
			{
				Instance.Helper.ModData.RemoveModData(card, "LimitedUsesToRestoreAfterBotching");
				Instance.KokoroApi.Limited.SetLimitedUses(state, card, limitedUsesToRestoreAfterBotching);
			}
			
			if (Instance.Helper.ModData.TryGetModData<int>(card, "FiniteUsesToRestoreAfterBotching", out var finiteUsesToRestoreAfterBotching))
			{
				Instance.Helper.ModData.RemoveModData(card, "FiniteUsesToRestoreAfterBotching");
				Instance.KokoroApi.Finite.SetFiniteUses(state, card, finiteUsesToRestoreAfterBotching);
			}
		});
	}

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.ResetAfterCombat)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_ResetAfterCombat_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.CanBeNegative)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_CanBeNegative_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_Set_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnAfterTurn_Prefix_First)), Priority.First)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Postfix)), Priority.Normal - 1)
		);
	}

	public double GetSmugBotchChance(State state, Ship ship, Card? card)
	{
		var smug = Instance.Api.GetSmug(state, ship);
		if (smug is null)
			return 0;
		if (ship.Get((Status)Instance.DoubleTimeStatus.Id!.Value) > 0)
			return 0;
		if (smug.Value > Instance.Api.GetMaxSmug(ship))
			return 1; // oversmug

		var chance = smug.Value < Instance.Api.GetMinSmug(ship) ? Constants.BotchChances[0] : Constants.BotchChances[smug.Value - Instance.Api.GetMinSmug(ship)];
		foreach (var hook in GetHooksWithProxies(Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			chance = hook.ModifySmugBotchChance(state, ship, card, chance);
		return Math.Clamp(chance, 0, 1);
	}

	public double GetSmugDoubleChance(State state, Ship ship, Card? card)
	{
		var smug = Instance.Api.GetSmug(state, ship);
		if (smug is null)
			return 0;
		if (ship.Get((Status)Instance.DoubleTimeStatus.Id!.Value) > 0)
			return 1;
		if (smug.Value > Instance.Api.GetMaxSmug(ship))
			return 0; // oversmug

		var chance = smug.Value < Instance.Api.GetMinSmug(ship) ? Constants.DoubleChances[0] : Constants.DoubleChances[smug.Value - Instance.Api.GetMinSmug(ship)];
		foreach (var hook in GetHooksWithProxies(Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			chance = hook.ModifySmugDoubleChance(state, ship, card, chance);
		return Math.Clamp(chance, 0, 1);
	}

	internal static SmugResult GetSmugResult(Rand rng, double botchChance, double doubleChance)
	{
		if (botchChance >= 1)
			return SmugResult.Botch;
		if (botchChance <= 0 && doubleChance >= 1)
			return SmugResult.Double;
		if (botchChance <= 0 && doubleChance <= 0)
			return SmugResult.Normal;

		var result = rng.Next();
		if (result < botchChance)
			return SmugResult.Botch;
		if (result < botchChance + doubleChance)
			return SmugResult.Double;

		return SmugResult.Normal;
	}

	public static Card GenerateAndTrackApology(State state, Combat combat, Rand rng, bool forDual = false, Type? ignoringType = null)
	{
		var timesApologyWasGiven = Instance.Helper.ModData.ObtainModData<Dictionary<string, int>>(combat, "TimesApologyWasGiven");
		var misprintedApologyArtifact = state.EnumerateAllArtifacts().OfType<MisprintedApologyArtifact>().FirstOrDefault();

		ApologyCard apology;
		if (!forDual && misprintedApologyArtifact is not null && !misprintedApologyArtifact.TriggeredThisTurn)
		{
			misprintedApologyArtifact.Pulse();
			misprintedApologyArtifact.TriggeredThisTurn = true;
			Narrative.SpeakBecauseOfAction(MG.inst.g, combat, $".{misprintedApologyArtifact.Key()}Trigger");
			var firstCard = GenerateAndTrackApology(state, combat, rng, forDual: true);
			var secondCard = GenerateAndTrackApology(state, combat, rng, forDual: true, ignoringType: firstCard.GetType());
			apology = new DualApologyCard
			{
				FirstCard = firstCard,
				SecondCard = secondCard
			};
		}
		else
		{
			var weightedRandom = new WeightedRandom<ApologyCard>();
			foreach (var apologyType in ModEntry.ApologyCards)
			{
				if (forDual && apologyType == typeof(DualApologyCard))
					continue;
				if (ignoringType is not null && apologyType == ignoringType)
					continue;

				apology = (ApologyCard)Activator.CreateInstance(apologyType)!;
				var weight = apology.GetApologyWeight(state, combat, timesApologyWasGiven.GetValueOrDefault(apologyType.FullName!));
				if (weight > 0)
					weightedRandom.Add(new(weight, apology));
			}
			apology = weightedRandom.Next(rng);
		}

		if (!forDual)
		{
			var totalApologies = timesApologyWasGiven.Values.Sum() - timesApologyWasGiven.GetValueOrDefault(typeof(DualApologyCard).FullName!) + 1;
			apology.ApologyFlavorText = $"<c={I18n.SogginsColor}>{string.Format(I18n.ApologyFlavorTexts[rng.NextInt() % I18n.ApologyFlavorTexts.Length], totalApologies)}</c>";
		}
		timesApologyWasGiven[apology.GetType().FullName!] = timesApologyWasGiven.GetValueOrDefault(apology.GetType().FullName!) + 1;
		return apology;
	}

	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod).CreateLdlocaInstruction(out var ldlocaData),
					ILMatches.Ldfld("exhaust"),
					ILMatches.Ldarg(4),
					ILMatches.Instruction(OpCodes.Or),
					ILMatches.Stloc<bool>(originalMethod).CreateLdlocaInstruction(out var ldlocaActuallyExhaust)
				)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld("infinite"),
					ILMatches.Brfalse,
					ILMatches.Ldloc<bool>(originalMethod),
					ILMatches.LdcI4(0),
					ILMatches.Instruction(OpCodes.Ceq),
					ILMatches.Br,
					ILMatches.LdcI4(0),
					ILMatches.Stloc<bool>(originalMethod).CreateLdlocaInstruction(out var ldlocaActuallyInfinite)
				)
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
					ldlocaActuallyExhaust,
					ldlocaActuallyInfinite,
					ldlocaData,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SmugStatusManager), nameof(Combat_TryPlayCard_Transpiler_ModifyActions)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static List<CardAction> Combat_TryPlayCard_Transpiler_ModifyActions(List<CardAction> actions, State state, Combat combat, Card card, bool playNoMatterWhatForFree, ref bool actuallyExhaust, ref bool actuallyInfinite, ref CardData data)
	{
		if (playNoMatterWhatForFree)
			return actions;

		var frogproofType = FrogproofManager.GetFrogproofType(state, card);
		if (frogproofType is FrogproofType.Innate or FrogproofType.InnateHiddenIfNotNeeded)
			return actions;

		var botchChance = Instance.Api.GetSmugBotchChance(state, state.ship, card);
		if (frogproofType == FrogproofType.Paid && botchChance > 0)
		{
			state.ship.Add((Status)Instance.FrogproofingStatus.Id!.Value, -1);
			return actions;
		}

		var doubleChance = Instance.Api.GetSmugDoubleChance(state, state.ship, card);
		var result = GetSmugResult(state.rngActions, botchChance, doubleChance);
		var swing = Math.Max(card.GetCurrentCost(state), 1);
		switch (result)
		{
			case SmugResult.Botch:
				actuallyExhaust = false;
				actuallyInfinite = false;
				data.singleUse = false;
				data.recycle = false;

				if (Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Instance.KokoroApi.Limited.Trait))
					Instance.Helper.ModData.SetModData(card, "LimitedUsesToRestoreAfterBotching", Instance.KokoroApi.Limited.GetLimitedUses(state, card));
				if (Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Instance.KokoroApi.Finite.Trait))
					Instance.Helper.ModData.SetModData(card, "FiniteUsesToRestoreAfterBotching", Instance.KokoroApi.Finite.GetFiniteUses(state, card));

				actions.Clear();
				actions.Add(new AStatus
				{
					status = (Status)Instance.BotchesStatus.Id!.Value,
					statusAmount = 1,
					targetPlayer = true,
					whoDidThis = card.GetMeta().deck
				});

				var isOversmug = Instance.Api.IsOversmug(state, state.ship);
				actions.Add(new AStatus
				{
					status = (Status)Instance.SmugStatus.Id!.Value,
					mode = isOversmug ? AStatusMode.Set : AStatusMode.Add,
					statusAmount = isOversmug ? Instance.Api.GetMinSmug(state.ship) : -swing,
					targetPlayer = true
				});

				var apologies = swing;
				foreach (var hook in Instance.SmugStatusManager.GetHooksWithProxies(Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
					apologies = hook.ModifyApologyAmountForBotchingBySmug(state, combat, card, apologies);

				actions.AddRange(Enumerable.Range(0, apologies).Select(_ => new AAddCard
				{
					card = GenerateAndTrackApology(state, combat, state.rngActions),
					destination = CardDestination.Hand
				}));

				foreach (var hook in Instance.SmugStatusManager.GetHooksWithProxies(Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
					hook.OnCardBotchedBySmug(state, combat, card);
				Instance.NarrativeManager.DidBotchCard = true;
				break;
			case SmugResult.Double:
				var toAdd = card.GetActionsOverridden(state, combat);

				var spawnActions = actions.SelectMany(Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively).OfType<ASpawn>().ToList();
				if (spawnActions.Count != 0)
					toAdd.Add(new ADroneMove { dir = spawnActions.Max(a => a.offset) - spawnActions.Min(a => a.offset) + 1 });

				var hasDoubleTime = state.ship.Get((Status)Instance.DoubleTimeStatus.Id!.Value) > 0;
				toAdd.Insert(0, new AShakeShip
				{
					statusPulse = hasDoubleTime ? (Status)Instance.DoubleTimeStatus.Id!.Value : (Status)Instance.SmugStatus.Id!.Value
				});
				if (!hasDoubleTime)
					toAdd.Insert(0, new AStatus
					{
						status = (Status)Instance.SmugStatus.Id!.Value,
						statusAmount = swing,
						targetPlayer = true
					});

				actions.InsertRange(0, toAdd);

				foreach (var hook in Instance.SmugStatusManager.GetHooksWithProxies(Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
					hook.OnCardDoubledBySmug(state, combat, card);
				Instance.NarrativeManager.DidDoubleCard = true;
				Instance.NarrativeManager.DidDoubleLaunchAction = spawnActions.Count != 0;
				break;
		}
		return actions;
	}

	private static void Ship_ResetAfterCombat_Postfix(Ship __instance)
	{
		if (MG.inst.g.state is not { } state)
			return;
		if (state.ship != __instance)
			return;

		Instance.Api.SetSmugEnabled(state, __instance, false);
	}

	private static void Ship_CanBeNegative_Postfix(Status status, ref bool __result)
	{
		if (status == (Status)Instance.SmugStatus.Id!.Value)
			__result = true;
	}

	private static void Ship_Set_Prefix(Ship __instance, Status status, ref int n)
	{
		if (status != (Status)Instance.SmugStatus.Id!.Value)
			return;
		n = Math.Clamp(n, Instance.Api.GetMinSmug(__instance), Instance.Api.GetMaxSmug(__instance) + 1);
	}

	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;

		if (s.ship.Get((Status)Instance.BidingTimeStatus.Id!.Value) > 0)
		{
			c.Queue(new AStatus
			{
				status = (Status)Instance.DoubleTimeStatus.Id!.Value,
				statusAmount = 1,
				targetPlayer = true
			});

			if (s.ship.Get(Status.timeStop) <= 0)
				c.Queue(new AStatus
				{
					status = (Status)Instance.BidingTimeStatus.Id!.Value,
					statusAmount = -1,
					targetPlayer = true
				});
		}

		var constantApologies = s.ship.Get((Status)Instance.ConstantApologiesStatus.Id!.Value);
		for (var i = 0; i < constantApologies; i++)
			c.Queue(new AAddCard
			{
				card = GenerateAndTrackApology(s, c, s.rngActions),
				destination = CardDestination.Hand,
				statusPulse = (Status)Instance.ConstantApologiesStatus.Id!.Value
			});
	}

	private static void Ship_OnAfterTurn_Prefix_First(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;

		if (s.ship.Get((Status)Instance.DoubleTimeStatus.Id!.Value) > 0)
			c.Queue(new AStatus
			{
				status = (Status)Instance.DoubleTimeStatus.Id!.Value,
				mode = AStatusMode.Set,
				statusAmount = 0,
				targetPlayer = true
			});
	}

	private static void Card_GetAllTooltips_Postfix(Card __instance, ref IEnumerable<Tooltip> __result)
	{
		if (__instance is not ApologyCard apology)
			return;
		if (string.IsNullOrEmpty(apology.ApologyFlavorText))
			return;
		var tooltipToYield = new TTText(apology.ApologyFlavorText);

		__result = ModifyTooltips(__result);

		IEnumerable<Tooltip> ModifyTooltips(IEnumerable<Tooltip> tooltips)
		{
			var yieldedFlavorText = false;

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
	}
}
