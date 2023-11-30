using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeIsaacArtifact : DuoArtifact
{
	public HashSet<int> MidrowObjectsToDestroy = new();

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			postfix: new HarmonyMethod(GetType(), nameof(ASpawn_Begin_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ADroneMove), nameof(ADroneMove.Begin)),
			postfix: new HarmonyMethod(GetType(), nameof(ADroneMove_Begin_Postfix))
		);
	}

	protected internal override void ApplyLatePatches(Harmony harmony)
	{
		base.ApplyLatePatches(harmony);
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.DoDestroyedEffect)),
			postfix: new HarmonyMethod(GetType(), nameof(StuffBase_DoDestroyedEffect_Postfix))
		);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		MidrowObjectsToDestroy.Clear();
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().FirstOrDefault(a => a is DrakeIsaacArtifact);

		foreach (var midrowObjectX in MidrowObjectsToDestroy)
		{
			if (!combat.stuff.TryGetValue(midrowObjectX, out var @object))
				continue;

			bool isInvincible = @object.Invincible();
			foreach (var someArtifact in state.EnumerateAllArtifacts())
			{
				if (someArtifact.ModifyDroneInvincibility(state, combat, @object) != true)
					continue;
				isInvincible = true;
				someArtifact.Pulse();
			}
			if (isInvincible)
				continue;

			combat.QueueImmediate(@object.GetActionsOnDestroyed(state, combat, wasPlayer: true, midrowObjectX));
			@object.DoDestroyedEffect(state, combat);
			combat.stuff.Remove(midrowObjectX);

			foreach (var someArtifact in state.EnumerateAllArtifacts())
				someArtifact.OnPlayerDestroyDrone(state, combat);
			artifact?.Pulse();
		}
		MidrowObjectsToDestroy.Clear();
	}

	private static void ASpawn_Begin_Postfix(ASpawn __instance, State s, Combat c)
	{
		if (!__instance.fromPlayer)
			return;
		if (!c.stuff.TryGetValue(__instance.thing.x, out var @object) || @object != __instance.thing)
			return;
		if (s.ship.Get(Status.heat) <= s.ship.heatMin)
			return;

		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		c.QueueImmediate(new AStatus
		{
			status = Status.heat,
			statusAmount = -1,
			targetPlayer = true
		});
		artifact.MidrowObjectsToDestroy.Add(__instance.thing.x);
		artifact.Pulse();
	}

	private static void ADroneMove_Begin_Postfix(ADroneMove __instance)
	{
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		artifact.MidrowObjectsToDestroy = artifact.MidrowObjectsToDestroy.Select(x => x + __instance.dir).ToHashSet();
	}

	private static void StuffBase_DoDestroyedEffect_Postfix(StuffBase __instance)
	{
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		artifact.MidrowObjectsToDestroy.Remove(__instance.x);
	}
}
