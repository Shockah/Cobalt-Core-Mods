using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Rerolls;

internal static class CardRewardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly List<Card> RerolledCards = [];

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetOffering)),
			postfix: new HarmonyMethod(typeof(CardRewardPatches), nameof(CardReward_GetOffering_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(CardReward).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<GetOffering>") && m.ReturnType == typeof(bool)),
			postfix: new HarmonyMethod(typeof(CardRewardPatches), nameof(CardReward_GetOffering_Delegate_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(ArtifactReward.Render)),
			postfix: new HarmonyMethod(typeof(CardRewardPatches), nameof(CardReward_Render_Postfix))
		);
	}

	private static void Reroll(CardReward menu, G g)
	{
		var artifact = g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null || artifact.LastCardOfferingConfig is not { } config)
			return;

		RerolledCards.AddRange(menu.cards);
		menu.cards = CardReward.GetOffering(g.state, config.Count, config.LimitDeck, config.BattleType, config.RarityOverride, config.OverrideUpgradeChances, config.MakeAllCardsTemporary, config.InCombat, config.Discount);
		artifact.RerollsLeft--;
		artifact.Pulse();
	}

	private static void CardReward_GetOffering_Postfix(State s, int count, Deck? limitDeck, BattleType battleType, Rarity? rarityOverride, bool? overrideUpgradeChances, bool makeAllCardsTemporary, bool inCombat, int discount)
	{
		var artifact = s.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		artifact.LastCardOfferingConfig = new(count, limitDeck, battleType, rarityOverride, overrideUpgradeChances, makeAllCardsTemporary, inCombat, discount);
		RerolledCards.Clear();
	}

	private static void CardReward_GetOffering_Delegate_Postfix(Card c, ref bool __result)
	{
		if (__result && RerolledCards.Any(rerolled => rerolled.Key() == c.Key() && rerolled.upgrade == c.upgrade))
			__result = false;
	}
	

	private static IEnumerable<CodeInstruction> CardReward_GetOffering_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void CardReward_Render_Postfix(CardReward __instance, G g)
	{
		if (__instance.ugpradePreview is not null)
			return;
		
		var artifact = g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null || artifact.RerollsLeft <= 0 || artifact.LastCardOfferingConfig is not { } config)
			return;
		if (config.InCombat)
			return;

		SharedArt.ButtonText(
			g,
			new Vec(210, 228),
			(UIKey)(UK)21370001,
			I18n.RerollButton,
			inactive: artifact.RerollsLeft <= 0,
			onMouseDown: new MouseDownHandler(() => Reroll(__instance, g)),
			platformButtonHint: Btn.Y
		);
		if (g.boxes.FirstOrDefault(b => b.key == new UIKey((UK)21370001)) is { } box)
			box.onInputPhase = new InputPhaseHandler(() =>
			{
				if (Input.GetGpDown(Btn.Y))
					Reroll(__instance, g);
			});
	}
}
