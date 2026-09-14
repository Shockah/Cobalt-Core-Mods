using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class BlastAction : AAttack, IRegisterable
{
	public int Range = 1;
	public int Direction;

	private int BlastDamage;
	
	private static ISpriteEntry Icon = null!;

	private static BlastAction? AttackContext;
	private static bool IsDuringBlastEffect;

	public override Icon? GetIcon(State s)
	{
		if (base.GetIcon(s) is { } icon)
			return icon with { path = Icon.Sprite };
		return new(Icon.Sprite, damage, Colors.redd);
	}

	public override void Begin(G g, State s, Combat c)
	{
		try
		{
			AttackContext = this;
			
			var attackCount = Direction == 0 ? Range * 2 + 1 : Range + 1;
			var splitDamage = damage / attackCount;
			var leftoverDamage = damage % attackCount;
			
			damage = splitDamage + leftoverDamage;
			BlastDamage = splitDamage;
			
			base.Begin(g, s, c);
		}
		finally
		{
			AttackContext = null;
		}
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Actions/Blast.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix)), priority: Priority.Low)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Part), nameof(Part.GetDamageModifier)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Part_GetDamageModifier_Postfix))
		);
	}
	
	private static void TriggerBlastIfNeeded(State state, Combat combat, int worldX, bool targetPlayer, bool hitMidrow)
	{
		if (AttackContext is null)
			return;

		var targetShip = targetPlayer ? state.ship : combat.otherShip;
		if (!hitMidrow && (targetShip.GetPartAtWorldX(worldX) is not { } part || part.type == PType.empty))
			return;

		AttackContext.timer *= 0.5;
		combat.QueueImmediate(new EffectAction
		{
			Source = AttackContext,
			TargetPlayer = targetPlayer,
			WorldX = hitMidrow ? worldX : null,
			LocalX = worldX - targetShip.x,
			Damage = AttackContext.BlastDamage,
			Range = AttackContext.Range,
			Direction = AttackContext.Direction,
			HitMidrow = hitMidrow,
		});
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<RaycastResult>(originalMethod).CreateLdlocInstruction(out var ldlocRaycastResult),
					ILMatches.Ldfld("hitDrone"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget),
				])
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_2).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_3),
					ldlocRaycastResult,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler_AfterDroneHitCheck))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void AAttack_Begin_Transpiler_AfterDroneHitCheck(State state, Combat combat, RaycastResult raycastResult)
	{
		if (raycastResult.fromDrone || raycastResult.hitShip || !raycastResult.hitDrone)
			return;
		if (AttackContext is not { } attack)
			return;
		TriggerBlastIfNeeded(state, combat, raycastResult.worldX, attack.targetPlayer, hitMidrow: true);
	}

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s, Combat c, int? maybeWorldGridX)
	{
		if (maybeWorldGridX is not { } worldGridX)
			return;
		TriggerBlastIfNeeded(s, c, worldGridX, targetPlayer: __instance.isPlayerShip, hitMidrow: false);
	}

	private static void Part_GetDamageModifier_Postfix(ref PDamMod __result)
	{
		if (AttackContext is null && !IsDuringBlastEffect)
			return;
		if (__result is PDamMod.weak or PDamMod.brittle)
			__result = PDamMod.none;
	}
	
	private sealed class EffectAction : CardAction
	{
		private const double SinglePartDuration = 0.2;

		public BlastAction? Source;
		public bool TargetPlayer;
		public int LocalX;
		public int? WorldX;
		public required int Damage;
		public int Range = 1;
		public int Direction;
		public bool HitMidrow;

		public override bool CanSkipTimerIfLastEvent()
			=> false;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = Range * SinglePartDuration;

			if (Range > 0)
				Run(g, s, c, 1);
		}

		public override void Update(G g, State s, Combat c)
		{
			var oldTimer = timer;
			base.Update(g, s, c);

			var maxTimer = Range * SinglePartDuration;
			var oldCurrentRange = Math.Min((int)((maxTimer - oldTimer) / SinglePartDuration), Range - 1);
			var newCurrentRange = Math.Min((int)((maxTimer - timer) / SinglePartDuration), Range - 1);

			for (var i = oldCurrentRange; i < newCurrentRange; i++)
				Run(g, s, c, i + 2);
		}

		private void Run(G g, State state, Combat combat, int offset)
		{
			try
			{
				IsDuringBlastEffect = true;
				
				var targetShip = TargetPlayer ? state.ship : combat.otherShip;
				var worldX = WorldX ?? (targetShip.x + LocalX);

				if (Direction <= 0)
					RunAt(worldX - offset);
				if (Direction >= 0)
					RunAt(worldX + offset);

				void RunForPartAt(int bitWorldX)
				{
					if (targetShip.GetPartAtWorldX(bitWorldX) is not { } part || part.type == PType.empty)
						return;

					if (part.stunModifier == PStunMod.stunnable || Source?.stunEnemy == true)
						new AStunPart { worldX = bitWorldX }.FullyRun(g, state, combat);
					
					var damageDone = targetShip.NormalDamage(state, combat, Damage, bitWorldX);
					var raycastResult = new RaycastResult
					{
						hitShip = true,
						worldX = bitWorldX
					};
					EffectSpawnerExt.HitEffect(g, TargetPlayer, raycastResult, damageDone);

					if (!TargetPlayer)
					{
						if (Source is not null)
							combat.otherShip.ai?.OnHitByAttack(state, combat, bitWorldX, Source);
						foreach (var artifact in state.EnumerateAllArtifacts())
							artifact.OnEnemyGetHit(state, combat, part);
					}
				}

				void RunForMidrowAt(int bitWorldX)
				{
					if (!combat.stuff.TryGetValue(bitWorldX, out var @object))
						return;

					var isInvincible = @object.Invincible();
					foreach (var artifact in state.EnumerateAllArtifacts())
					{
						if (artifact.ModifyDroneInvincibility(state, combat, @object) == true)
						{
							isInvincible = true;
							artifact.Pulse();
						}
					}

					var damageDone = new DamageDone
					{
						hitShield = @object.bubbleShield,
						hitHull = !@object.bubbleShield
					};
					var raycastResult = new RaycastResult
					{
						hitDrone = true,
						worldX = bitWorldX
					};
					EffectSpawnerExt.HitEffect(g, TargetPlayer, raycastResult, damageDone);

					if (@object.bubbleShield)
					{
						@object.bubbleShield = false;
						Audio.Play(Event.Hits_ShieldPop);
					}
					else if (isInvincible)
					{
						combat.QueueImmediate(@object.GetActionsOnShotWhileInvincible(state, combat, !TargetPlayer, Damage));
					}
					else
					{
						combat.DestroyDroneAt(state, bitWorldX, !TargetPlayer);
					}
				}

				void RunAt(int bitWorldX)
				{
					if (HitMidrow)
						RunForMidrowAt(bitWorldX);
					else
						RunForPartAt(bitWorldX);
				}
			}
			finally
			{
				IsDuringBlastEffect = false;
			}
		}
	}
}