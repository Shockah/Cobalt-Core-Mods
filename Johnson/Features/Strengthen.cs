using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Johnson;

internal static class StrengthenExt
{
	public static int GetStrengthen(this Card self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(self, "Strengthen");

	public static void SetStrengthen(this Card self, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(self, "Strengthen", value);

	public static void AddStrengthen(this Card self, int value)
		=> self.SetStrengthen(self.GetStrengthen() + value);
}

internal sealed class StrengthenManager
{
	public StrengthenManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(GetType(), nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(GetType(), nameof(Card_GetAllTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(GetType(), nameof(Card_GetActionsOverridden_Postfix))
		);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
			{
				if (card.GetStrengthen() == 0)
					continue;
				card.SetStrengthen(0);
			}
		}, 0);
	}

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod).ExtractLabels(out var labels).Anchor(out var findAnchor),
					ILMatches.Ldfld("buoyant"),
					ILMatches.Brfalse
				)
				.Find(
					ILMatches.Ldloc<Vec>(originalMethod).CreateLdlocInstruction(out var ldlocVec),
					ILMatches.Ldfld("y"),
					ILMatches.LdcI4(8),
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocaInstruction(out var ldlocaCardTraitIndex),
					ILMatches.Instruction(OpCodes.Dup),
					ILMatches.LdcI4(1),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod)
				)
				.Anchors().PointerMatcher(findAnchor)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					ldlocaCardTraitIndex,
					ldlocVec,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_RenderCardTraitIfNeeded)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Card_Render_Transpiler_RenderCardTraitIfNeeded(Card card, ref int cardTraitIndex, Vec vec)
	{
		if (card.GetStrengthen() <= 0)
			return;
		Draw.Sprite(ModEntry.Instance.StrengthenIcon.Sprite, vec.x, vec.y - 8 * cardTraitIndex++);
	}

	private static void Card_GetAllTooltips_Postfix(Card __instance, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;

		var strengthen = __instance.GetStrengthen();
		if (strengthen <= 0)
			return;

		CustomTTGlossary MakeTooltip()
			=> new(
				CustomTTGlossary.GlossaryType.cardtrait,
				() => ModEntry.Instance.StrengthenIcon.Sprite,
				() => ModEntry.Instance.Localizations.Localize(["cardTrait", "strengthen", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["cardTrait", "strengthen", "description"], new { Damage = strengthen }),
				key: $"{ModEntry.Instance.Package.Manifest.UniqueName}::Strengthen"
			);

		IEnumerable<Tooltip> ModifyTooltips(IEnumerable<Tooltip> tooltips)
		{
			bool yieldedCardTrait = false;

			foreach (var tooltip in tooltips)
			{
				if (!yieldedCardTrait && tooltip is TTGlossary glossary && glossary.key.StartsWith("cardtrait.") && glossary.key != "cardtrait.unplayable")
				{
					yield return MakeTooltip();
					yieldedCardTrait = true;
				}
				yield return tooltip;
			}

			if (!yieldedCardTrait)
				yield return MakeTooltip();
		}

		__result = ModifyTooltips(__result);
	}

	private static void Card_GetActionsOverridden_Postfix(Card __instance, ref List<CardAction> __result)
	{
		var strengthen = __instance.GetStrengthen();
		if (strengthen <= 0)
			return;

		foreach (var baseAction in __result)
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.Actions.GetWrappedCardActionsRecursively(baseAction))
				if (wrappedAction is AAttack attack)
					attack.damage += strengthen;
	}
}
