using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.EnemyPromotion;

internal sealed class MediumFighterPromotion : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Api.RegisterPromotedEnemy<MediumFighter>(EnemyPromotion.Promote);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MediumFighter), nameof(AI.BuildShipForSelf)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(BuildShipForSelf_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MediumFighter), nameof(AI.PickNextIntent)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PickNextIntent_Postfix))
		);
	}

	private static void BuildShipForSelf_Postfix(AI __instance, State s, Ship __result)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "Promoted"))
			return;

		// var hullToAdd = (int)Math.Ceiling(__result.hullMax * 0.2);
		// __result.hullMax += hullToAdd;
		// __result.hull += hullToAdd;
	}

	private static void PickNextIntent_Postfix(AI __instance, State s, Combat c, Ship ownShip, EnemyDecision __result)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "Promoted"))
			return;
		if (__result.intents is null)
			return;
		
		foreach (var intent in __result.intents)
			if (intent is IntentAttack attackIntent)
				attackIntent.cardOnHit = new ColorlessTrash();
	}
}