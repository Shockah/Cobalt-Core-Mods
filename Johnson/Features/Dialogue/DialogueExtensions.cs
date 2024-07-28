using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Johnson;

internal static class DialogueExt
{
	public static int GetMinShieldLostThisTurn(this StoryNode node)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(node, "MinShieldLostThisTurn");

	public static StoryNode SetMinShieldLostThisTurn(this StoryNode node, int value)
	{
		ModEntry.Instance.Helper.ModData.SetModData(node, "MinShieldLostThisTurn", value);
		return node;
	}

	public static int GetShieldLostThisTurn(this StoryVars vars)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(vars, "ShieldLostThisTurn");

	public static void SetShieldLostThisTurn(this StoryVars vars, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(vars, "ShieldLostThisTurn", value);

	public static HashSet<int> GetLastCardIdsInDeck(this Combat combat)
		=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "LastCardIdsInDeck");
}

internal sealed class DialogueExtensions
{
	public DialogueExtensions()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.ResetAfterEndTurn)),
			postfix: new HarmonyMethod(GetType(), nameof(StoryVars_ResetAfterEndTurn_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryNode), nameof(StoryNode.Filter)),
			postfix: new HarmonyMethod(GetType(), nameof(StoryNode_Filter_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AStatus_Begin_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(AStatus_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(GetType(), nameof(Ship_NormalDamage_Prefix)), priority: Priority.First),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(GetType(), nameof(Ship_NormalDamage_Postfix)), priority: Priority.Last)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!card.GetDataWithOverrides(state).recycle)
				return;
			combat.QueueImmediate(new ADummyAction { dialogueSelector = $".{ModEntry.Instance.Package.Manifest.UniqueName}::PlayedRecycle" });
		}, double.NegativeInfinity);
	}

	private static void StoryVars_ResetAfterEndTurn_Postfix(StoryVars __instance)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(__instance, "ShieldLostThisTurn");

	private static void StoryNode_Filter_Postfix(StoryNode n, State s, ref bool __result)
	{
		if (!__result)
			return;

		if (s.storyVars.GetShieldLostThisTurn() < n.GetMinShieldLostThisTurn())
		{
			__result = false;
			return;
		}
	}

	private static void AStatus_Begin_Prefix(AStatus __instance, State s, ref int __state)
		=> __state = __instance.targetPlayer ? s.ship.Get(__instance.status) : 0;


	private static void AStatus_Begin_Postfix(AStatus __instance, State s, Combat c, ref int __state)
	{
		if (!__instance.targetPlayer)
			return;

		if (__instance.status == ModEntry.Instance.JohnsonCharacter.MissingStatus.Status && __state > 0 && s.ship.Get(ModEntry.Instance.JohnsonCharacter.MissingStatus.Status) <= 0)
			c.QueueImmediate(new ADummyAction { dialogueSelector = $".{ModEntry.Instance.Package.Manifest.UniqueName}::ReturningFromMissing" });
	}

	private static void Ship_NormalDamage_Prefix(Ship __instance, ref int __state)
		=> __state = __instance.Get(Status.shield) + __instance.Get(Status.tempShield);

	private static void Ship_NormalDamage_Postfix(Ship __instance, State s, ref int __state)
	{
		var newShields = __instance.Get(Status.shield) + __instance.Get(Status.tempShield);
		if (newShields >= __state)
			return;

		s.storyVars.SetShieldLostThisTurn(s.storyVars.GetShieldLostThisTurn() + (__state - newShields));
	}

	private static void Combat_Update_Postfix(Combat __instance, G g)
	{
		var currentCardsInDeck = g.state.GetAllCards().Select(card => card.uuid).ToHashSet();
		var lastCardsInDeck = __instance.GetLastCardIdsInDeck();

		foreach (var cardId in currentCardsInDeck)
		{
			if (lastCardsInDeck.Contains(cardId))
				continue;
			if (g.state.FindCard(cardId) is not { } card)
				continue;

			var meta = card.GetMeta();
			if (meta.deck != ModEntry.Instance.JohnsonDeck.Deck && NewRunOptions.allChars.Contains(meta.deck) && card.GetDataWithOverrides(g.state).temporary)
				__instance.QueueImmediate(new ADummyAction { dialogueSelector = $".{ModEntry.Instance.Package.Manifest.UniqueName}::NewNonJohnsonNonTrashTempCard" });
		}

		ModEntry.Instance.Helper.ModData.SetModData(__instance, "LastCardIdsInDeck", currentCardsInDeck);
	}
}