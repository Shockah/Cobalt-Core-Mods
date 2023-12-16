using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Soggins;

public sealed class FrogproofManager : HookManager<IFrogproofHook>
{
	private const string IsRunWithSmugKey = "IsRunWithSmug";

	private static ModEntry Instance => ModEntry.Instance;

	internal FrogproofManager() : base()
	{
		Register(FrogproofCardTraitFrogproofHook.Instance, 0);
		Register(NonVanillaNonCharacterCardFrogproofHook.Instance, 1);
		Register(FrogproofingFrogproofHook.Instance, -10);
	}

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(typeof(FrogproofManager), nameof(Card_Render_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(typeof(FrogproofManager), nameof(Card_GetAllTooltips_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			postfix: new HarmonyMethod(typeof(FrogproofManager), nameof(Ship_Set_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(State), nameof(State.EndRun)),
			postfix: new HarmonyMethod(typeof(FrogproofManager), nameof(State_EndRun_Postfix))
		);
	}

	public bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> GetFrogproofType(state, combat, card, context) != FrogproofType.None;

	public FrogproofType GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
	{
		foreach (var hook in GetHooksWithProxies(Instance.KokoroApi, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.GetFrogproofType(state, combat, card, context);
			if (hookResult == FrogproofType.None)
				return FrogproofType.None;
			else if (hookResult != null)
				return hookResult.Value;
		}
		return FrogproofType.None;
	}

	public IFrogproofHook? GetHandlingHook(State state, Combat? combat, Card card, FrogproofHookContext context = FrogproofHookContext.Action)
	{
		foreach (var hook in GetHooksWithProxies(Instance.KokoroApi, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.GetFrogproofType(state, combat, card, context);
			if (hookResult == FrogproofType.None)
				return null;
			else if (hookResult != null)
				return hook;
		}
		return null;
	}

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldfld("buoyant"),
					ILMatches.Brfalse
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.ExtractLabels(out var labels)
				.AnchorPointer(out Guid findAnchor)
				.Find(
					ILMatches.Ldloc<Vec>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldfld("y"),
					ILMatches.LdcI4(8),
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Instruction(OpCodes.Dup),
					ILMatches.LdcI4(1),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocVec)
				.Advance(3)
				.CreateLdlocaInstruction(out var ldlocaCardTraitIndex)
				.PointerMatcher(findAnchor)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldarg_0),
					ldlocaCardTraitIndex,
					ldlocVec,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(FrogproofManager), nameof(Card_Render_Transpiler_RenderFrogproofIfNeeded)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Card_Render_Transpiler_RenderFrogproofIfNeeded(G g, State? state, Card card, ref int cardTraitIndex, Vec vec)
	{
		state ??= g.state;

		var frogproofType = Instance.FrogproofManager.GetFrogproofType(state, state.route as Combat, card, FrogproofHookContext.Rendering);
		if (frogproofType == FrogproofType.None)
			return;
		// state is usually DB.fakeState here; use g.state instead
		if (frogproofType == FrogproofType.InnateHiddenIfNotNeeded && !Instance.KokoroApi.ObtainExtensionData<bool>(g.state, IsRunWithSmugKey))
			return;
		Draw.Sprite((Spr)Instance.FrogproofSprite.Id!.Value, vec.x, vec.y - 8 * cardTraitIndex++);
	}

	private static void Card_GetAllTooltips_Postfix(Card __instance, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;

		var frogproofType = Instance.FrogproofManager.GetFrogproofType(s, s.route as Combat, __instance, FrogproofHookContext.Rendering);
		if (frogproofType == FrogproofType.None)
			return;
		// state is usually DB.fakeState here; use StateExt.Instance instead
		if (frogproofType == FrogproofType.InnateHiddenIfNotNeeded && !Instance.KokoroApi.ObtainExtensionData<bool>(StateExt.Instance ?? s, IsRunWithSmugKey))
			return;

		static IEnumerable<Tooltip> ModifyTooltips(IEnumerable<Tooltip> tooltips)
		{
			bool yieldedFrogproof = false;

			foreach (var tooltip in tooltips)
			{
				if (!yieldedFrogproof && tooltip is TTGlossary glossary && glossary.key.StartsWith("cardtrait.") && glossary.key != "cardtrait.unplayable")
				{
					yield return Instance.Api.FrogproofCardTraitTooltip;
					yieldedFrogproof = true;
				}
				yield return tooltip;
			}

			if (!yieldedFrogproof)
				yield return Instance.Api.FrogproofCardTraitTooltip;
		}

		__result = ModifyTooltips(__result);
	}

	private static void Ship_Set_Postfix(Ship __instance, Status status, int n)
	{
		if (StateExt.Instance is not { } state || state.ship != __instance)
			return;
		if (status != (Status)Instance.SmuggedStatus.Id!.Value)
			return;
		Instance.KokoroApi.SetExtensionData(state, IsRunWithSmugKey, n > 0);
	}

	private static void State_EndRun_Postfix(State __instance)
		=> Instance.KokoroApi.RemoveExtensionData(__instance, IsRunWithSmugKey);
}

public sealed class FrogproofCardTraitFrogproofHook : IFrogproofHook
{
	public static FrogproofCardTraitFrogproofHook Instance { get; private set; } = new();

	private FrogproofCardTraitFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> card is IFrogproofCard frogproofCard && frogproofCard.IsFrogproof(state, combat) ? FrogproofType.Innate : null;

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}

public sealed class NonVanillaNonCharacterCardFrogproofHook : IFrogproofHook
{
	public static NonVanillaNonCharacterCardFrogproofHook Instance { get; private set; } = new();

	private NonVanillaNonCharacterCardFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
	{
		var meta = card.GetMeta();
		if (NewRunOptions.allChars.Contains(meta.deck))
			return null;
		if (meta.deck is Deck.colorless or Deck.catartifact or Deck.soggins or Deck.dracula or Deck.tooth or Deck.ephemeral)
			return null;
		return FrogproofType.InnateHiddenIfNotNeeded;
	}

	public void PayForFrogproof(State state, Combat? combat, Card card) { }
}

public sealed class FrogproofingFrogproofHook : IFrogproofHook
{
	public static FrogproofingFrogproofHook Instance { get; private set; } = new();

	private FrogproofingFrogproofHook() { }

	public FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context)
		=> context == FrogproofHookContext.Action && state.ship.Get((Status)ModEntry.Instance.FrogproofingStatus.Id!.Value) > 0 ? FrogproofType.Paid : null;

	public void PayForFrogproof(State state, Combat? combat, Card card)
		=> state.ship.Add((Status)ModEntry.Instance.FrogproofingStatus.Id!.Value, -1);
}