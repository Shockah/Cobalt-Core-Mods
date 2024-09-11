using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public Tooltip GetScorchingTooltip(int? value = null)
		=> value is null
			? new CustomTTGlossary(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => I18n.ScorchingGlossaryName, () => I18n.ScorchingGlossaryAltDescription)
			: new CustomTTGlossary(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => I18n.ScorchingGlossaryName, () => I18n.ScorchingGlossaryDescription, [() => value.Value]);

	public int GetScorchingStatus(State state, Combat combat, StuffBase @object)
		=> TryGetExtensionData(@object, ModEntry.ScorchingTag, out int value) ? value : 0;

	public void SetScorchingStatus(State state, Combat combat, StuffBase @object, int value)
	{
		var oldValue = GetScorchingStatus(state, combat, @object);
		SetExtensionData(@object, ModEntry.ScorchingTag, value);
		foreach (var hook in MidrowScorchingManager.Instance.GetHooksWithProxies(this, state.EnumerateAllArtifacts()))
			hook.OnScorchingChange(combat, @object, oldValue, value);
	}

	public void AddScorchingStatus(State state, Combat combat, StuffBase @object, int value)
		=> SetScorchingStatus(state, combat, @object, Math.Max(GetScorchingStatus(state, combat, @object) + value, 0));

	public void RegisterMidrowScorchingHook(IMidrowScorchingHook hook, double priority)
		=> MidrowScorchingManager.Instance.Register(hook, priority);

	public void UnregisterMidrowScorchingHook(IMidrowScorchingHook hook)
		=> MidrowScorchingManager.Instance.Unregister(hook);
}

public sealed class MidrowScorchingManager : HookManager<IMidrowScorchingHook>
{
	internal static readonly MidrowScorchingManager Instance = new();

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.DrawWithHilight)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StuffBase_DrawWithHilight_Postfix))
		);
	}

	internal static void SetupLate(IHarmony harmony)
	{
		harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StuffBase_GetTooltips_Postfix))
		);
		harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetActionsOnDestroyed)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StuffBase_GetActionsOnDestroyed_Postfix))
		);
	}

	internal void OnPlayerTurnStart(State state, Combat combat)
	{
		foreach (var @object in combat.stuff.Values)
		{
			if (ModEntry.Instance.Api.GetScorchingStatus(state, combat, @object) <= 0)
				continue;

			var isInvincible = @object.Invincible();
			foreach (var someArtifact in state.EnumerateAllArtifacts())
			{
				if (someArtifact.ModifyDroneInvincibility(state, combat, @object) != true)
					continue;
				isInvincible = true;
				someArtifact.Pulse();
			}
			if (isInvincible)
				continue;

			if (@object.bubbleShield)
			{
				@object.bubbleShield = false;
				continue;
			}

			ModEntry.Instance.Api.SetScorchingStatus(state, combat, @object, 0);
			combat.QueueImmediate(@object.GetActionsOnDestroyed(state, combat, wasPlayer: true, @object.x));
			@object.DoDestroyedEffect(state, combat);
			combat.stuff.Remove(@object.x);

			foreach (var someArtifact in state.EnumerateAllArtifacts())
				someArtifact.OnPlayerDestroyDrone(state, combat);
		}
	}

	internal void ModifyMidrowObjectDestroyedActions(State state, Combat combat, StuffBase @object, bool wasPlayer, List<CardAction> actions)
	{
		var scorching = ModEntry.Instance.Api.GetScorchingStatus(state, combat, @object);
		if (scorching <= 0)
			return;

		actions.Add(new AStatus
		{
			status = Status.heat,
			statusAmount = scorching,
			targetPlayer = wasPlayer
		});
	}

	internal void ModifyMidrowObjectTooltips(StuffBase @object, List<Tooltip> tooltips)
	{
		if (MG.inst.g.state is not { } state)
			return;
		if (state.route is not Combat combat)
			return;

		var scorching = ModEntry.Instance.Api.GetScorchingStatus(state, combat, @object);
		if (scorching <= 0)
			return;

		tooltips.Add(ModEntry.Instance.Api.GetScorchingTooltip(scorching));
	}

	internal void OnDrawWithHilight(StuffBase @object, G g, Spr sprite, Vec v, bool flipX, bool flipY)
	{
		if (g.state.route is not Combat combat)
			return;
		if (ModEntry.Instance.Api.GetScorchingStatus(g.state, combat, @object) <= 0)
			return;

		var color = new Color(1, 0.35, 0).fadeAlpha(Math.Sin(MG.inst.g.time * Math.PI * 2) * 0.5 + 0.5);
		Draw.Sprite(sprite, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color);
		Draw.Sprite(sprite, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color, blend: BlendState.Additive);
	}
	
	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (!__instance.isPlayerShip)
			return;
		Instance.OnPlayerTurnStart(s, c);
	}
	
	private static void StuffBase_DrawWithHilight_Postfix(StuffBase __instance, G g, Spr id, Vec v, bool flipX, bool flipY)
		=> Instance.OnDrawWithHilight(__instance, g, id, v, flipX, flipY);

	private static void StuffBase_GetTooltips_Postfix(StuffBase __instance, ref List<Tooltip> __result)
		=> Instance.ModifyMidrowObjectTooltips(__instance, __result);

	private static void StuffBase_GetActionsOnDestroyed_Postfix(StuffBase __instance, State __0, Combat __1, bool __2 /* wasPlayer */, ref List<CardAction>? __result)
	{
		__result ??= [];
		Instance.ModifyMidrowObjectDestroyedActions(__0, __1, __instance, __2, __result);
	}
}
