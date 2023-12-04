using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using static System.Collections.Specialized.BitVector32;
using Microsoft.Xna.Framework.Graphics;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeIsaacArtifact : DuoArtifact
{
	public HashSet<int> ScorchingMidrowObjectPositions = new();

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
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.DrawWithHilight)),
			postfix: new HarmonyMethod(GetType(), nameof(StuffBase_DrawWithHilight_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AMissileHit), nameof(AMissileHit.Update)),
			transpiler: new HarmonyMethod(GetType(), nameof(AMissileHit_Update_Transpiler))
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
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetTooltips)),
			postfix: new HarmonyMethod(GetType(), nameof(StuffBase_GetTooltips_Postfix))
		);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		ScorchingMidrowObjectPositions.Clear();
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().FirstOrDefault(a => a is DrakeIsaacArtifact);

		foreach (var midrowObjectX in ScorchingMidrowObjectPositions.ToList())
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

			artifact?.Pulse();

			if (@object.bubbleShield)
			{
				@object.bubbleShield = false;
				continue;
			}

			combat.QueueImmediate(@object.GetActionsOnDestroyed(state, combat, wasPlayer: true, midrowObjectX));
			@object.DoDestroyedEffect(state, combat);
			combat.stuff.Remove(midrowObjectX);

			foreach (var someArtifact in state.EnumerateAllArtifacts())
				someArtifact.OnPlayerDestroyDrone(state, combat);
			ScorchingMidrowObjectPositions.Remove(midrowObjectX);
		}
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
		artifact.ScorchingMidrowObjectPositions.Add(__instance.thing.x);
		artifact.Pulse();
	}

	private static void ADroneMove_Begin_Postfix(ADroneMove __instance)
	{
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		artifact.ScorchingMidrowObjectPositions = artifact.ScorchingMidrowObjectPositions.Select(x => x + __instance.dir).ToHashSet();
	}

	private static void StuffBase_DrawWithHilight_Postfix(StuffBase __instance, G g, Spr id, Vec v, bool flipX, bool flipY)
	{
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		if (!artifact.ScorchingMidrowObjectPositions.Contains(__instance.x))
			return;

		var color = new Color(1, 0.35, 0).fadeAlpha(Math.Sin(Instance.TotalGameTime.TotalSeconds * Math.PI * 2) * 0.5 + 0.5);
		Draw.Sprite(id, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color);
		Draw.Sprite(id, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color, blend: BlendState.Additive);
	}

	private static IEnumerable<CodeInstruction> AMissileHit_Update_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<bool>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Brfalse
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(DrakeIsaacArtifact), nameof(AMissileHit_Update_Transpiler_ApplyHeat)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void AMissileHit_Update_Transpiler_ApplyHeat(AMissileHit action, State state, Combat combat)
	{
		var artifact = state.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		if (!artifact.ScorchingMidrowObjectPositions.Contains(action.worldX))
			return;

		combat.QueueImmediate(new AStatus
		{
			status = Status.heat,
			statusAmount = 2,
			targetPlayer = false
		});
		artifact.ScorchingMidrowObjectPositions.Remove(action.worldX);
	}

	private static void StuffBase_DoDestroyedEffect_Postfix(StuffBase __instance)
	{
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		artifact.ScorchingMidrowObjectPositions.Remove(__instance.x);
	}

	private static void StuffBase_GetTooltips_Postfix(Missile __instance, ref List<Tooltip> __result)
	{
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		if (!artifact.ScorchingMidrowObjectPositions.Contains(__instance.x))
			return;

		__result.Add(I18n.ScorchingGlossary);
	}
}
