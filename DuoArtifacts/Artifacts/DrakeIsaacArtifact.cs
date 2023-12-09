using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeIsaacArtifact : DuoArtifact
{
	private static readonly string ScorchingTag = $"{typeof(ModEntry).Namespace}.MidrowTag.Scorching";

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
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetTooltips)),
			postfix: new HarmonyMethod(GetType(), nameof(StuffBase_GetTooltips_Postfix))
		);
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetActionsOnDestroyed)),
			postfix: new HarmonyMethod(GetType(), nameof(StuffBase_GetActionsOnDestroyed_Postfix))
		);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		var artifact = StateExt.Instance?.EnumerateAllArtifacts().FirstOrDefault(a => a is DrakeIsaacArtifact);

		foreach (var @object in combat.stuff.Values)
		{
			if (!Instance.KokoroApi.IsMidrowObjectTagged(combat, @object, ScorchingTag))
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

			Instance.KokoroApi.UntagMidrowObject(combat, @object, ScorchingTag);
			combat.QueueImmediate(@object.GetActionsOnDestroyed(state, combat, wasPlayer: true, @object.x));
			@object.DoDestroyedEffect(state, combat);
			combat.stuff.Remove(@object.x);

			foreach (var someArtifact in state.EnumerateAllArtifacts())
				someArtifact.OnPlayerDestroyDrone(state, combat);
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
		Instance.KokoroApi.TagMidrowObject(c, __instance.thing, ScorchingTag);
		artifact.Pulse();
	}

	private static void StuffBase_DrawWithHilight_Postfix(StuffBase __instance, G g, Spr id, Vec v, bool flipX, bool flipY)
	{
		if (g.state.route is not Combat combat)
			return;
		if (!Instance.KokoroApi.IsMidrowObjectTagged(combat, __instance, ScorchingTag))
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

	private static void AMissileHit_Update_Transpiler_ApplyHeat(AMissileHit action, Combat combat)
	{
		if (!combat.stuff.TryGetValue(action.worldX, out var @object))
			return;
		if (!Instance.KokoroApi.IsMidrowObjectTagged(combat, @object, ScorchingTag))
			return;

		combat.QueueImmediate(new AStatus
		{
			status = Status.heat,
			statusAmount = 3,
			targetPlayer = action.targetPlayer
		});
	}

	private static void StuffBase_GetTooltips_Postfix(StuffBase __instance, ref List<Tooltip> __result)
	{
		if (StateExt.Instance?.route is not Combat combat)
			return;
		if (!Instance.KokoroApi.IsMidrowObjectTagged(combat, __instance, ScorchingTag))
			return;

		__result.Add(I18n.ScorchingGlossary);
	}

	private static void StuffBase_GetActionsOnDestroyed_Postfix(StuffBase __instance, Combat __1, bool __2 /* wasPlayer */, ref List<CardAction>? __result)
	{
		if (!Instance.KokoroApi.IsMidrowObjectTagged(__1, __instance, ScorchingTag))
			return;

		__result ??= new();
		__result.Add(new AStatus
		{
			status = Status.heat,
			statusAmount = 3,
			targetPlayer = __2
		});
	}
}
