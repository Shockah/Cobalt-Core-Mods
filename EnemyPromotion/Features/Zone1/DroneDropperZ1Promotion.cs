using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.EnemyPromotion;

internal sealed class DroneDropperZ1Promotion : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Api.RegisterPromotedEnemy<DroneDropperZ1>(EnemyPromotion.Promote);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DroneDropperZ1), nameof(AI.BuildShipForSelf)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(BuildShipForSelf_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DroneDropperZ1), nameof(AI.GetModifier)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetModifier_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DroneDropperZ1), nameof(AI.PickNextIntent)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PickNextIntent_Postfix))
		);
	}

	private static void BuildShipForSelf_Postfix(AI __instance/*, State s, Ship __result*/)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "Promoted"))
			return;

		// var hullToAdd = (int)Math.Ceiling(__result.hullMax * 0.2);
		// __result.hullMax += hullToAdd;
		// __result.hull += hullToAdd;
	}

	private static void GetModifier_Postfix(AI __instance, State s, Combat c, ref FightModifier? __result)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "Promoted"))
			return;
		
		__result = new PromotedFightModifier { leftForcefield = 5, rightForcefield = 15 };
	}

	private static void PickNextIntent_Postfix(AI __instance/*, State s, Combat c, Ship ownShip, EnemyDecision __result*/)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "Promoted"))
			return;
	}

	private sealed class PromotedFightModifier : FightModifier
	{
		public int? leftForcefield;
		public int? rightForcefield;
		
		public override Spr Icon()
			=> StableSpr.icons_env_wall;
		
		public override List<CardAction> GetCombatStartActions(State s, Combat c)
		{
			// TODO: fix right forcefield being off by 1
			// AEnvironmentTurn, should be `rff - ship.x - ship.parts.Count - 1`
			c.leftForcefield = leftForcefield;
			c.rightForcefield = rightForcefield;
			return [];
		}
	}
}