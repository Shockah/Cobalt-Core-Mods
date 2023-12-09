using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Shockah.Shared;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal static class StuffBasePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.DrawWithHilight)),
			postfix: new HarmonyMethod(typeof(StuffBasePatches), nameof(StuffBase_DrawWithHilight_Postfix))
		);
	}

	public static void ApplyLate(Harmony harmony)
	{
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetTooltips)),
			postfix: new HarmonyMethod(typeof(StuffBasePatches), nameof(StuffBase_GetTooltips_Postfix))
		);
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetActionsOnDestroyed)),
			postfix: new HarmonyMethod(typeof(StuffBasePatches), nameof(StuffBase_GetActionsOnDestroyed_Postfix))
		);
	}

	private static void StuffBase_DrawWithHilight_Postfix(StuffBase __instance, G g, Spr id, Vec v, bool flipX, bool flipY)
	{
		if (g.state.route is not Combat combat)
			return;
		if (Instance.Api.GetScorchingStatus(combat, __instance) <= 0)
			return;

		var color = new Color(1, 0.35, 0).fadeAlpha(Math.Sin(Instance.TotalGameTime.TotalSeconds * Math.PI * 2) * 0.5 + 0.5);
		Draw.Sprite(id, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color);
		Draw.Sprite(id, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color, blend: BlendState.Additive);
	}

	private static void StuffBase_GetTooltips_Postfix(StuffBase __instance, ref List<Tooltip> __result)
	{
		if (StateExt.Instance?.route is not Combat combat)
			return;

		var scorching = Instance.Api.GetScorchingStatus(combat, __instance);
		if (scorching <= 0)
			return;

		__result.Add(Instance.Api.GetScorchingTooltip(scorching));
	}

	private static void StuffBase_GetActionsOnDestroyed_Postfix(StuffBase __instance, Combat __1, bool __2 /* wasPlayer */, ref List<CardAction>? __result)
	{
		var scorching = Instance.Api.GetScorchingStatus(__1, __instance);
		if (scorching <= 0)
			return;

		__result ??= new();
		__result.Add(new AStatus
		{
			status = Status.heat,
			statusAmount = scorching,
			targetPlayer = __2
		});
	}
}
