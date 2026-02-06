using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nickel;

namespace Shockah.Bloch;

internal static class DiscardedVariableHintExtensions
{
	extension(Combat combat)
	{
		public int DiscardedCardCount
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(combat, "DiscardedCardCount");
			set => ModEntry.Instance.Helper.ModData.SetModData(combat, "DiscardedCardCount", value);
		}
	}
}

internal sealed class DiscardedVariableHintManager
{
	private static Card? LastCardPlayed;
	
	public DiscardedVariableHintManager()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToDiscard)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToDiscard_Postfix))
		);
	}
	
	private static void Combat_TryPlayCard_Prefix(Card card)
		=> LastCardPlayed = card;

	private static void Combat_TryPlayCard_Finalizer()
		=> LastCardPlayed = null;

	private static void Combat_SendCardToDiscard_Postfix(Combat __instance, Card card)
	{
		if (!__instance.isPlayerTurn)
			return;
		if (card == LastCardPlayed)
			return;

		__instance.DiscardedCardCount++;
	}
}

internal sealed class DiscardedVariableHint : AVariableHint
{
	public int ExtraAmount;
	
	public DiscardedVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new(StableSpr.icons_discardCard, null, Colors.white);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action::{ModEntry.Instance.Package.Manifest.UniqueName}::DiscardedVariableHint.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(
					["action", "DiscardedVariableHint", s.route is Combat ? "stateful" : "stateless"],
					new { Count = (s.route is Combat combat ? combat.DiscardedCardCount : 0) + ExtraAmount }
				)
			},
		];
}