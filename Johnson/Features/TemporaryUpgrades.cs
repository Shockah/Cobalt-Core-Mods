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

internal static class TemporaryUpgradesExt
{
	public static bool IsTemporarilyUpgraded(this Card self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(self, "IsTemporarilyUpgraded");

	public static void SetTemporarilyUpgraded(this Card self, bool value)
		=> ModEntry.Instance.Helper.ModData.SetModData(self, "IsTemporarilyUpgraded", value);
}

internal sealed class TemporaryUpgradeManager
{
	public TemporaryUpgradeManager()
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

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			state.rewardsQueue.Queue(new ARemoveTemporaryUpgrades());
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
		if (card.upgrade == Upgrade.None || !card.IsTemporarilyUpgraded())
			return;
		Draw.Sprite(ModEntry.Instance.TemporaryUpgradeIcon.Sprite, vec.x, vec.y - 8 * cardTraitIndex++);
	}

	private static void Card_GetAllTooltips_Postfix(Card __instance, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		if (!showCardTraits)
			return;
		if (__instance.upgrade == Upgrade.None || !__instance.IsTemporarilyUpgraded())
			return;

		static CustomTTGlossary MakeTooltip()
			=> new(
				CustomTTGlossary.GlossaryType.cardtrait,
				() => ModEntry.Instance.TemporaryUpgradeIcon.Sprite,
				() => ModEntry.Instance.Localizations.Localize(["cardTrait", "temporaryUpgrade", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["cardTrait", "temporaryUpgrade", "description"])
			);

		static IEnumerable<Tooltip> ModifyTooltips(IEnumerable<Tooltip> tooltips)
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

	public sealed class ARemoveTemporaryUpgrades : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			foreach (var card in s.GetAllCards())
			{
				if (!card.IsTemporarilyUpgraded())
					continue;
				card.SetTemporarilyUpgraded(false);
				card.upgrade = Upgrade.None;
			}
		}
	}
}
