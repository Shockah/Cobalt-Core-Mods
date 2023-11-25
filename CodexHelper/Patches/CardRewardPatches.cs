using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Shockah.CodexHelper;

internal static class CardRewardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static bool IsRenderingCardRewardScreen = false;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.Render)),
			prefix: new HarmonyMethod(typeof(CardRewardPatches), nameof(CardReward_Render_Prefix)),
			finalizer: new HarmonyMethod(typeof(CardRewardPatches), nameof(CardReward_Render_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(typeof(CardRewardPatches), nameof(Card_Render_Transpiler))
		);
	}

	private static void CardReward_Render_Prefix()
		=> IsRenderingCardRewardScreen = true;

	private static void CardReward_Render_Finalizer()
		=> IsRenderingCardRewardScreen = false;

	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldstr("cardRarity.common"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardRewardPatches), nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
				)

				.Find(
					ILMatches.Ldstr("cardRarity.uncommon"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardRewardPatches), nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
				)

				.Find(
					ILMatches.Ldstr("cardRarity.rare"),
					ILMatches.Instruction(OpCodes.Ldstr),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardRewardPatches), nameof(Card_Render_Transpiler_ModifyRarityTextIfNeeded)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static string Card_Render_Transpiler_ModifyRarityTextIfNeeded(string rarityText, Card card, G g)
	{
		if (IsRenderingCardRewardScreen && !g.state.storyVars.cardsOwned.Contains(card.Key()))
			rarityText = $"<c=textMain>{I18n.MissingFromCodex}</c> {rarityText}";
		return rarityText;
	}
}