using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Soggins;

internal sealed class FrogproofManager
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			postfix: new HarmonyMethod(typeof(FrogproofManager), nameof(Ship_Set_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(FrogproofManager), nameof(State_PopulateRun_Postfix_First)), priority: Priority.First)
		);
	}

	public static bool IsFrogproof(State state, Card card)
		=> GetFrogproofType(state, card) != FrogproofType.None;

	public static FrogproofType GetFrogproofType(State state, Card card)
	{
		if (Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Instance.FrogproofTrait))
			return FrogproofType.Innate;
		if (state.ship.Get((Status)Instance.FrogproofingStatus.Id!.Value) > 0)
			return FrogproofType.Paid;
		return FrogproofType.None;
	}

	private static void Ship_Set_Postfix(Ship __instance, Status status, int n)
	{
		if (MG.inst.g.state is not { } state || state.ship != __instance)
			return;
		if (n == 0)
			return;
		if (status != (Status)Instance.SmugStatus.Id!.Value)
			return;
		Instance.Api.SetSmugEnabled(state, __instance);
	}

	private static void State_PopulateRun_Postfix_First(State __instance)
		=> Instance.Helper.ModData.RemoveModData(__instance, ApiImplementation.IsRunWithSmugKey);
}