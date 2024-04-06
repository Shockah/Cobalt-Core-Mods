using FSPRO;
using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal static class ChargeExt
{
	public static FireChargeAction? GetInProgressFireChargeAction(this Combat combat)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<FireChargeAction>(combat, "InProgressFireChargeAction");

	public static void SetInProgressFireChargeAction(this Combat combat, FireChargeAction? action)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(combat, "InProgressFireChargeAction", action);

	public static DynaCharge? GetStickedCharge(this Part part)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<DynaCharge>(part, "StickedCharge");

	public static void SetStickedCharge(this Part part, DynaCharge? charge)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(part, "StickedCharge", charge);
}

internal sealed class ChargeManager
{
	public ChargeManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDrones)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_RenderDrones_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderShipUI)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_RenderShipUI_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderPartUI)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_RenderPartUI_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix))
		);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerTakeNormalDamage), (State state, Combat combat, Part? part) =>
		{
			TriggerChargeIfAny(state, combat, part, targetPlayer: true);
		}, -1);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnEnemyGetHit), (State state, Combat combat, Part? part) =>
		{
			TriggerChargeIfAny(state, combat, part, targetPlayer: false);
		}, -1);
	}

	internal static void TriggerChargeIfAny(State state, Combat combat, Part? part, bool targetPlayer)
	{
		if (part is not { } nonNullPart)
			return;
		if (nonNullPart.GetStickedCharge() is not { } charge)
			return;

		var targetShip = targetPlayer ? state.ship : combat.otherShip;
		nonNullPart.SetStickedCharge(null);
		charge.OnTrigger(state, combat, targetShip, nonNullPart);
	}

	private static void RenderShipCharges(G g, State state, Combat combat, Ship ship)
	{
		for (var i = 0; i < ship.parts.Count; i++)
			RenderShipCharge(g, state, combat, ship, i);
	}

	private static void RenderShipCharge(G g, State state, Combat combat, Ship ship, int partIndex)
	{
		if (ship.parts[partIndex] is not { } part || part.type == PType.empty)
			return;
		if (part.GetStickedCharge() is not { } charge)
			return;
		RenderAnyCharge(g, state, combat, ship, partIndex, charge);
	}

	private static void RenderAnyCharge(G g, State state, Combat combat, Ship ship, int partIndex, DynaCharge charge)
	{
		var box = g.Push(null, new Rect(partIndex * 16 + 7.5, 53.5));
		charge.Render(g, state, combat, ship, ship.x + partIndex, box.rect.xy);
		g.Pop();
	}

	private static void Combat_RenderDrones_Postfix(Combat __instance, G g)
	{
		if (__instance.GetInProgressFireChargeAction() is not { } inProgressFireChargeAction || inProgressFireChargeAction.VolleyX is not { } x)
			return;

		var targetShip = inProgressFireChargeAction.TargetPlayer ? g.state.ship : __instance.otherShip;
		var partIndex = x + inProgressFireChargeAction.Offset - targetShip.x;

		g.Push(null, new Rect(targetShip.GetX(g.state), 12) + Combat.arenaPos + __instance.GetCamOffset());
		RenderAnyCharge(g, g.state, __instance, targetShip, partIndex, inProgressFireChargeAction.Charge);
		g.Pop();
	}

	private static void Ship_RenderShipUI_Prefix(Ship __instance, G g, Vec v, bool positionSelf, bool isPreview)
	{
		if (isPreview)
			return;
		if (g.state.route is not Combat combat)
			return;

		g.Push(null, new Rect(positionSelf ? __instance.GetX(g.state) : 0) + v);
		RenderShipCharges(g, g.state, combat, __instance);
		g.Pop();
	}

	private static void Ship_RenderPartUI_Prefix(G g, Combat? combat, Part part, int localX, string keyPrefix, bool isPreview)
	{
		if (isPreview || combat is null)
			return;
		if (part.GetStickedCharge() is not { } charge)
			return;

		if (g.boxes.FirstOrDefault(b => b.key == new UIKey(StableUK.part, localX, keyPrefix)) is not { } box)
			return;
		if (!box.IsHover())
			return;

		g.tooltips.Add(g.tooltips.pos, charge.GetTooltips(g.state));
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, ref int __result)
	{
		if (action is not FireChargeAction fireAction)
			return true;

		var box = g.Push();

		if (!dontDraw)
			Draw.Sprite(fireAction.Offset switch
			{
				< 0 => StableSpr.icons_spawnOffsetLeft,
				> 0 => StableSpr.icons_spawnOffsetRight,
				_ => StableSpr.icons_spawn
			}, box.rect.x + __result, box.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		__result += 8;

		if (fireAction.Offset != 0)
		{
			__result += 2;

			if (!dontDraw)
				BigNumbers.Render(Math.Abs(fireAction.Offset), box.rect.x + __result, box.rect.y, color: action.disabled ? Colors.disabledDrone : Colors.drone);
			__result += Math.Abs(fireAction.Offset).ToString().Length * 6;
		}

		__result += 2;

		var icon = fireAction.Charge.GetIcon(state);
		if (!dontDraw)
			Draw.Sprite(icon, box.rect.x + __result, box.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		__result += 9;

		g.Pop();

		return false;
	}
}

public sealed class FireChargeAction : CardAction
{
	private const double ShipDistanceFromMidrow = 40;
	private const double OuterSpaceDistanceFromMidrow = 200;
	private const double DistancePerSecond = OuterSpaceDistanceFromMidrow;

	public required DynaCharge Charge;
	public int Offset;
	public int? VolleyX;
	public bool TargetPlayer;

	public override bool CanSkipTimerIfLastEvent()
		=> false;

	public override List<Tooltip> GetTooltips(State s)
	{
		for (var partIndex = 0; partIndex < s.ship.parts.Count; partIndex++)
		{
			var part = s.ship.parts[partIndex];
			if (part.type != PType.missiles || !part.active)
				continue;

			part.hilight = true;
			if (s.route is Combat combat && combat.stuff.TryGetValue(s.ship.x + partIndex, out var @object))
				@object.hilight = 2;
		}

		List<Tooltip> tooltips = [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => Offset switch
				{
					< 0 => StableSpr.icons_spawnOffsetLeft,
					> 0 => StableSpr.icons_spawnOffsetRight,
					_ => StableSpr.icons_spawn
				},
				() => ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "name", Offset switch
				{
					< 0 => "OffsetLeft",
					> 0 => "OffsetRight",
					_ => "Normal"
				}]),
				() => ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "description", Offset switch
				{
					< 0 => "OffsetLeft",
					> 0 => "OffsetRight",
					_ => "Normal"
				}], new { Offset = Math.Abs(Offset) }),
				key: $"{ModEntry.Instance.Package.Manifest.UniqueName}::FireCharge"
			)
		];
		tooltips.AddRange(Charge.GetTooltips(s));
		return tooltips;
	}

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		var ownerShip = TargetPlayer ? c.otherShip : s.ship;
		var targetShip = TargetPlayer ? s.ship : c.otherShip;

		var x = VolleyX ?? 0;
		if (VolleyX is null)
		{
			var bayPartIndexes = Enumerable.Range(0, ownerShip.parts.Count)
				.Where(i =>
				{
					var part = ownerShip.parts[i];
					if (part.type != PType.missiles)
						return false;
					if (!part.active)
						return false;
					return true;
				})
				.ToList();

			switch (bayPartIndexes.Count)
			{
				case 0:
					timer = 0.4;
					return;
				case 1:
					x = ownerShip.x + bayPartIndexes[0];
					VolleyX = x;
					break;
				default:
					c.QueueImmediate(
						bayPartIndexes
							.Select(i => ownerShip.x + i)
							.Select(x =>
							{
								var action = Mutil.DeepCopy(this);
								action.VolleyX = x;
								return action;
							})
					);
					timer = 0;
					return;
			}
		}

		c.SetInProgressFireChargeAction(this);

		var worldX = x + Offset;
		var initialPosition = TargetPlayer ? -ShipDistanceFromMidrow : ShipDistanceFromMidrow;
		double finalPosition;
		if (c.stuff.ContainsKey(worldX))
			finalPosition = 0;
		else if (targetShip.GetPartAtWorldX(worldX) is not null)
			finalPosition = TargetPlayer ? ShipDistanceFromMidrow : -ShipDistanceFromMidrow;
		else
			finalPosition = TargetPlayer ? OuterSpaceDistanceFromMidrow : -OuterSpaceDistanceFromMidrow;

		timer = Math.Abs(finalPosition - initialPosition) / DistancePerSecond;
		Audio.Play(Event.Drones_MissileLaunch);
		if (ownerShip.GetPartAtWorldX(worldX) is { } firingPart)
			firingPart.pulse = 1;
	}

	public override void Update(G g, State s, Combat c)
	{
		if (timer <= 0)
			return;

		base.Update(g, s, c);
		c.SetInProgressFireChargeAction(timer <= 0 ? null : this);

		if (VolleyX is not { } x)
			return;

		var worldX = x + Offset;
		if (!c.stuff.TryGetValue(worldX, out var @object))
			@object = null;

		var targetShip = TargetPlayer ? s.ship : c.otherShip;
		var initialPosition = TargetPlayer ? -ShipDistanceFromMidrow : ShipDistanceFromMidrow;
		double finalPosition;
		if (@object is not null)
			finalPosition = 0;
		else if ((targetShip.GetPartAtWorldX(worldX)?.type ?? PType.empty) != PType.empty)
			finalPosition = TargetPlayer ? ShipDistanceFromMidrow : -ShipDistanceFromMidrow;
		else
			finalPosition = TargetPlayer ? OuterSpaceDistanceFromMidrow : -OuterSpaceDistanceFromMidrow;

		var duration = Math.Abs(finalPosition - initialPosition) / DistancePerSecond;
		var progress = 1.0 - Math.Clamp(timer / duration, 0, 1);
		Charge.YOffset = initialPosition + (finalPosition - initialPosition) * progress;

		if (timer > 0)
			return;

		if (@object is not null)
		{
			var isInvincible = @object.Invincible();
			foreach (var artifact in s.EnumerateAllArtifacts())
			{
				if (artifact.ModifyDroneInvincibility(s, c, @object) == true)
				{
					isInvincible = true;
					artifact.Pulse();
				}
			}

			if (@object.bubbleShield)
				@object.bubbleShield = false;
			else if (isInvincible)
				c.QueueImmediate(@object.GetActionsOnBonkedWhileInvincible(s, c, !TargetPlayer, new DynaChargeFakeStuff()));
			else
				c.DestroyDroneAt(s, worldX, !TargetPlayer);
			Audio.Play(Event.Hits_DroneCollision);
		}
		else if (targetShip.GetPartAtWorldX(worldX) is { } part && part.type != PType.empty)
		{
			if (part.GetStickedCharge() is { } existingCharge)
			{
				Audio.Play(Event.Hits_DroneCollision);
				part.SetStickedCharge(null);
				// reversed order - charges are expected to QueueImmediate their actions, which reverses their order
				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.KokoroApi, s.EnumerateAllArtifacts()))
					hook.OnChargeTrigger(s, c, targetShip, worldX);
				Charge.OnTrigger(s, c, targetShip, part);
				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.KokoroApi, s.EnumerateAllArtifacts()))
					hook.OnChargeTrigger(s, c, targetShip, worldX);
				existingCharge.OnTrigger(s, c, targetShip, part);
			}
			else
			{
				Audio.Play(Event.Hits_ShieldHit);
				part.SetStickedCharge(Charge);

				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.KokoroApi, s.EnumerateAllArtifacts()))
					hook.OnChargeSticked(s, c, targetShip, worldX);
			}
		}
	}
}

public abstract class DynaCharge
{
	public double YOffset = 0;

	public abstract Spr GetIcon(State state);

	public virtual Spr? GetLightsIcon(State state)
		=> null;

	public virtual void Render(G g, State state, Combat combat, Ship ship, int worldX, Vec position)
	{
		var icon = GetIcon(state);
		var texture = SpriteLoader.Get(icon)!;
		Draw.Sprite(icon, position.x - texture.Width / 2.0, position.y - texture.Height / 2.0 + YOffset);

		if (GetLightsIcon(state) is not { } lightsIcon)
			return;
		texture = SpriteLoader.Get(lightsIcon)!;
		var color = Color.Lerp(Colors.white, Colors.black, (ModEntry.Instance.KokoroApi.TotalGameTime.TotalSeconds + position.x / 160.0) % 1.0);
		Draw.Sprite(lightsIcon, position.x - texture.Width / 2.0, position.y - texture.Height / 2.0 + YOffset, color: color);
	}

	public virtual List<Tooltip> GetTooltips(State state)
		=> [];

	public virtual void OnTrigger(State state, Combat combat, Ship ship, Part part)
	{
	}
}

internal sealed class DynaChargeFakeStuff : StuffBase
{
}