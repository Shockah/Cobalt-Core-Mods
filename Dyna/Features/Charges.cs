using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

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
	private const UK PlayerChargeUK = (UK)2137031;
	private const UK EnemyChargeUK = (UK)2137032;
	private const UK IncomingChargeUK = (UK)2137033;

	public ChargeManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(State), nameof(State.Render)),
			transpiler: new HarmonyMethod(GetType(), nameof(State_Render_Transpiler))
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

	private static void TriggerChargeIfAny(State state, Combat combat, Part? part, bool targetPlayer)
	{
		if (part is not { } nonNullPart)
			return;
		if (nonNullPart.GetStickedCharge() is not { } charge)
			return;

		var targetShip = targetPlayer ? state.ship : combat.otherShip;
		nonNullPart.SetStickedCharge(null);
		charge.OnTrigger(state, combat, targetShip, nonNullPart);
	}

	private static void RenderCharges(G g, State state)
	{
		if (state.route is not Combat combat)
			return;

		void RenderAnyCharge(Ship ship, int partIndex, DynaCharge charge, bool isIncoming = false)
		{
			var box = g.Push(new UIKey(isIncoming ? IncomingChargeUK : (ship.isPlayerShip ? PlayerChargeUK : EnemyChargeUK), ship.x + partIndex), new Rect((ship.xLerped + partIndex) * 16 + 12.5, 90.5));
			charge.Render(g, state, combat, ship, ship.x + partIndex, box.rect.xy);
			g.Pop();
		}

		void RenderShipCharge(Ship ship, int partIndex)
		{
			if (ship.parts[partIndex] is not { } part || part.type == PType.empty)
				return;
			if (part.GetStickedCharge() is not { } charge)
				return;
			RenderAnyCharge(ship, partIndex, charge);
		}

		void RenderShipCharges(Ship ship)
		{
			for (var i = 0; i < ship.parts.Count; i++)
				RenderShipCharge(ship, i);
		}

		var rect = default(Rect) + Combat.arenaPos + combat.GetCamOffset();
		g.Push(null, rect);

		RenderShipCharges(state.ship);
		RenderShipCharges(combat.otherShip);
		if (combat.GetInProgressFireChargeAction() is { } inProgressFireChargeAction && inProgressFireChargeAction.VolleyX is { } x)
		{
			var targetShip = inProgressFireChargeAction.TargetPlayer ? state.ship : combat.otherShip;
			var partIndex = x - targetShip.x;
			RenderAnyCharge(targetShip, partIndex, inProgressFireChargeAction.Charge, isIncoming: true);
		}

		g.Pop();
	}

	private static IEnumerable<CodeInstruction> State_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<MapRoute>(originalMethod).ExtractLabels(out var labels),
					ILMatches.Ldarg(1),
					ILMatches.Call("Render")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RenderCharges)))
				)
				.Find(
					ILMatches.Ldarg(0).ExtractLabels(out labels),
					ILMatches.Ldfld("route"),
					ILMatches.Ldarg(1),
					ILMatches.Call("Render")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RenderCharges)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not FireChargeAction fireAction)
			return true;

		var box = g.Push();

		if (!dontDraw)
			Draw.Sprite(StableSpr.icons_spawn, box.rect.x + __result, box.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		__result += 9;

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
			if (part.type == PType.missiles && part.active)
				part.hilight = true;

			if (s.route is Combat combat && combat.stuff.TryGetValue(s.ship.x + partIndex, out var @object))
				@object.hilight = 2;
		}

		List<Tooltip> tooltips = [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => StableSpr.icons_spawn,
				() => ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "description"]),
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
			Audio.Play(Event.Hits_DroneCollision);
		}
		else if (targetShip.GetPartAtWorldX(worldX) is { } part && part.type != PType.empty)
		{
			if (part.GetStickedCharge() is { } existingCharge)
			{
				Audio.Play(Event.Hits_DroneCollision);
				part.SetStickedCharge(null);
				existingCharge.OnTrigger(s, c, targetShip, part);
				Charge.OnTrigger(s, c, targetShip, part);
			}
			else
			{
				Audio.Play(Event.Hits_ShieldHit);
				part.SetStickedCharge(Charge);
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