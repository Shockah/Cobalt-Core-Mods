using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
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

	public static IDynaCharge? GetStickedCharge(this Part part)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<IDynaCharge>(part, "StickedCharge");

	public static void SetStickedCharge(this Part part, IDynaCharge? charge)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(part, "StickedCharge", charge);
}

internal sealed class ChargeManager
{
	internal static ISpriteEntry FireChargeIcon { get; private set; } = null!;
	internal static ISpriteEntry FireChargeLeftIcon { get; private set; } = null!;
	internal static ISpriteEntry FireChargeRightIcon { get; private set; } = null!;

	private static AAttack? AttackContext;
	private static StuffBase? BonkContext;

	public ChargeManager()
	{
		FireChargeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/FireCharge.png"));
		FireChargeLeftIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/FireChargeLeft.png"));
		FireChargeRightIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/FireChargeRight.png"));

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
			postfix: new HarmonyMethod(GetType(), nameof(Ship_RenderPartUI_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer)),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Transpiler))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Football), nameof(Football.GetActionsOnBonkedWhileInvincible)),
			prefix: new HarmonyMethod(GetType(), nameof(Football_GetActionsOnBonkedWhileInvincible_Prefix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Football), nameof(Football.GetActionsOnShotWhileInvincible)),
			prefix: new HarmonyMethod(GetType(), nameof(Football_GetActionsOnShotWhileInvincible_Prefix))
		);
	}

	internal static bool TriggerChargeIfAny(State state, Combat combat, Part part, bool targetPlayer)
	{
		if (part.GetStickedCharge() is not { } charge)
			return false;

		var targetShip = targetPlayer ? state.ship : combat.otherShip;
		var worldX = targetShip.x + targetShip.parts.IndexOf(part);
		part.SetStickedCharge(null);
		foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			hook.OnChargeTrigger(state, combat, targetShip, worldX);
		charge.OnTrigger(state, combat, targetShip, part);
		return true;
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

		charge.YOffset = FireChargeAction.ShipDistanceFromMidrow * (ship.isPlayerShip ? 1 : -1);
		g.Push(null, new Rect(0, (ship.isPlayerShip ? 6 : -6) * part.pulse).round());
		RenderAnyCharge(g, state, combat, ship, part.xLerped ?? partIndex, charge);
		g.Pop();
	}

	private static void RenderAnyCharge(G g, State state, Combat combat, Ship ship, double partIndex, IDynaCharge charge)
	{
		var box = g.Push(null, new Rect(partIndex * 16 + 7.5, 53.5));
		charge.Render(g, state, combat, ship, (int)(ship.x + partIndex), box.rect.xy);
		g.Pop();
	}

	internal static void DefaultRenderChargeImplementation(IDynaCharge charge, G g, State state, Vec position)
	{
		var icon = charge.GetIcon(state);
		var texture = SpriteLoader.Get(icon)!;
		Draw.Sprite(icon, position.x - texture.Width / 2.0, position.y - texture.Height / 2.0 + charge.YOffset);

		if (charge.GetLightsIcon(state) is not { } lightsIcon)
			return;
		texture = SpriteLoader.Get(lightsIcon)!;
		var color = Color.Lerp(Colors.white, Colors.black, (MG.inst.g.time + position.x / 160.0) % 1.0);
		Draw.Sprite(lightsIcon, position.x - texture.Width / 2.0, position.y - texture.Height / 2.0 + charge.YOffset, color: color);
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

	private static void Ship_RenderPartUI_Postfix(G g, Combat? combat, Part part, int localX, string keyPrefix, bool isPreview)
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
				< 0 => FireChargeLeftIcon.Sprite,
				> 0 => FireChargeRightIcon.Sprite,
				_ => FireChargeIcon.Sprite
			}, box.rect.x + __result, box.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		__result += 8;

		if (fireAction.Offset != 0)
		{
			__result += 2;

			if (!dontDraw)
				BigNumbers.Render(Math.Abs(fireAction.Offset), box.rect.x + __result, box.rect.y, color: action.disabled ? new Color("4B4B4B") : new Color("DBDBDB"));
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

	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<RaycastResult>(originalMethod).CreateLdlocInstruction(out var ldlocRaycastResult),
					ILMatches.Ldfld("hitShip"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				)
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					ldlocRaycastResult,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler_TriggerChargeIfAny)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void AAttack_Begin_Transpiler_TriggerChargeIfAny(AAttack attack, State state, Combat combat, RaycastResult raycastResult)
	{
		if (raycastResult.hitDrone || !raycastResult.hitShip)
			return;
		if (attack.isBeam)
			return;

		var targetShip = attack.targetPlayer ? state.ship : combat.otherShip;
		if (targetShip.GetPartAtWorldX(raycastResult.worldX) is not { } part || part.type == PType.empty)
			return;

		TriggerChargeIfAny(state, combat, part, attack.targetPlayer);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_NormalDamage_Postfix(Ship __instance, State s, Combat c, int? maybeWorldGridX)
	{
		if (AttackContext is not null)
			return;
		if (maybeWorldGridX is not { } worldGridX)
			return;
		if (__instance.GetPartAtWorldX(worldGridX) is not { } part || part.type == PType.empty)
			return;
		TriggerChargeIfAny(s, c, part, targetPlayer: __instance.isPlayerShip);
	}

	private static void Football_GetActionsOnBonkedWhileInvincible_Prefix(StuffBase thing)
		=> BonkContext = thing;

	private static void Football_GetActionsOnShotWhileInvincible_Prefix(ref int damage)
	{
		if (BonkContext is not { } thing)
			return;

		BonkContext = null;
		if (thing is DynaChargeFakeDrone charge)
			damage += charge.ExtraDamage;
	}
}

public sealed class FireChargeAction : CardAction
{
	internal const double ShipDistanceFromMidrow = 40;
	private const double OuterSpaceDistanceFromMidrow = 200;
	private const double DistancePerSecond = OuterSpaceDistanceFromMidrow;

	public required IDynaCharge Charge;
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
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::FireCharge")
			{
				Icon = Offset switch
				{
					< 0 => ChargeManager.FireChargeLeftIcon.Sprite,
					> 0 => ChargeManager.FireChargeRightIcon.Sprite,
					_ => ChargeManager.FireChargeIcon.Sprite
				},
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "name", Offset switch
				{
					< 0 => "OffsetLeft",
					> 0 => "OffsetRight",
					_ => "Normal"
				}]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "FireCharge", "description", Offset switch
				{
					< 0 => "OffsetLeft",
					> 0 => "OffsetRight",
					_ => "Normal"
				}], new { Offset = Math.Abs(Offset) })
			}
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
		else if (targetShip.GetPartAtWorldX(worldX) is { } targetPart && targetPart.type != PType.empty)
			finalPosition = TargetPlayer ? ShipDistanceFromMidrow : -ShipDistanceFromMidrow;
		else
			finalPosition = TargetPlayer ? OuterSpaceDistanceFromMidrow : -OuterSpaceDistanceFromMidrow;

		timer = Math.Abs(finalPosition - initialPosition) / DistancePerSecond;
		Audio.Play(Event.Drones_MissileLaunch);
		if (ownerShip.GetPartAtWorldX(worldX) is { } firingPart)
			firingPart.pulse = 1;

		foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
			hook.OnChargeFired(s, c, targetShip, worldX);
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
		var @object = c.stuff.GetValueOrDefault(worldX);

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
			var dynaChargeFakeDrone = new DynaChargeFakeDrone { ExtraDamage = Charge.BonkDamage - 2 };
			var outcome = ASpawn.GetCollisionOutcome(dynaChargeFakeDrone, @object);
			@object.bubbleShield = false;

			if (outcome is ASpawn.Outcome.BothDie or ASpawn.Outcome.LaunchedWins)
				c.DestroyDroneAt(s, worldX, !TargetPlayer);
			else if (@object.Invincible())
				c.QueueImmediate(@object.GetActionsOnBonkedWhileInvincible(s, c, !TargetPlayer, dynaChargeFakeDrone));

			Audio.Play(Event.Hits_DroneCollision);
		}
		else if (targetShip.GetPartAtWorldX(worldX) is { } part && part.type != PType.empty)
		{
			if (part.GetStickedCharge() is { } existingCharge)
			{
				Audio.Play(Event.Hits_DroneCollision);
				part.SetStickedCharge(null);
				// reversed order - charges are expected to QueueImmediate their actions, which reverses their order
				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
					hook.OnChargeTrigger(s, c, targetShip, worldX);
				Charge.OnTrigger(s, c, targetShip, part);
				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
					hook.OnChargeTrigger(s, c, targetShip, worldX);
				existingCharge.OnTrigger(s, c, targetShip, part);
			}
			else
			{
				Audio.Play(Event.Hits_ShieldHit);
				part.SetStickedCharge(Charge);

				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
					hook.OnChargeSticked(s, c, targetShip, worldX);
			}
		}
	}
}

internal sealed class DynaChargeFakeDrone : FakeDrone
{
	public int ExtraDamage;
}

public abstract class BaseDynaCharge(string key) : IDynaCharge
{
	public string Key() => key;
	public double YOffset { get; set; }
	public virtual int BonkDamage { get; } = 2;

	public abstract Spr GetIcon(State state);

	public virtual Spr? GetLightsIcon(State state)
		=> null;

	public virtual void Render(G g, State state, Combat combat, Ship ship, int worldX, Vec position)
		=> ModEntry.Instance.Api.DefaultRenderChargeImplementation(this, g, state, combat, ship, worldX, position);

	public virtual IEnumerable<Tooltip> GetTooltips(State state)
		=> [];

	public virtual void OnTrigger(State state, Combat combat, Ship ship, Part part)
	{
	}
}