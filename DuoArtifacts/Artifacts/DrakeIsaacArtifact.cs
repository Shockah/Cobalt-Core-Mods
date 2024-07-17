using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeIsaacArtifact : DuoArtifact
{
	protected internal override void ApplyLatePatches(IHarmony harmony)
	{
		base.ApplyLatePatches(harmony);
		harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetActions)),
			postfix: new HarmonyMethod(GetType(), nameof(StuffBase_GetActions_Postfix))
		);
	}

	public override int? GetDisplayNumber(State s)
	{
		if (s.route is not Combat combat)
			return base.GetDisplayNumber(s);

		int drones = combat.stuff.Values.Count(o => o is AttackDrone or ShieldDrone && o.fromPlayer);
		int heatToGain = drones - 1;
		return Math.Max(heatToGain, 0);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		if (MG.inst.g.state?.route is not Combat combat)
			return base.GetExtraTooltips();

		foreach (var @object in combat.stuff.Values)
		{
			if (@object is not AttackDrone or ShieldDrone)
				continue;
			if (!@object.fromPlayer)
				continue;
			@object.hilight = 2;
		}
		return base.GetExtraTooltips();
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		int drones = combat.stuff.Values.Count(o => o is AttackDrone or ShieldDrone && o.fromPlayer);
		int heatToGain = drones - 1;

		if (heatToGain <= 0)
			return;
		Pulse();
		combat.Queue(new AStatus
		{
			status = Status.heat,
			statusAmount = heatToGain,
			targetPlayer = true
		});
	}

	private static void StuffBase_GetActions_Postfix(StuffBase __instance, State s, ref List<CardAction>? __result)
	{
		if (__instance is not (AttackDrone or ShieldDrone))
			return;
		if (!__instance.fromPlayer)
			return;
		if (__result is null || __result.Count == 0)
			return;

		var artifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is DrakeIsaacArtifact);
		if (artifact is null)
			return;

		if ((__result ?? []).Count == 0)
			return;

		artifact.Pulse();
		__result = [..__result, ..__result];
	}
}
