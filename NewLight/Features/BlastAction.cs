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

public static class BlastActionExt
{
	extension(AAttack attack)
	{
		public bool HideBlastInCardRendering
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(attack, "HideBlastInCardRendering");
			set => ModEntry.Instance.Helper.ModData.SetModData(attack, "HideBlastInCardRendering", value);
		}
		
		public bool CanPrimaryBlastDamageCrit
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(attack, "CanPrimaryBlastDamageCrit");
			set => ModEntry.Instance.Helper.ModData.SetModData(attack, "CanPrimaryBlastDamageCrit", value);
		}
		
		public bool CanSecondaryBlastDamageCrit
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(attack, "CanSecondaryBlastDamageCrit");
			set => ModEntry.Instance.Helper.ModData.SetModData(attack, "CanSecondaryBlastDamageCrit", value);
		}
		
		public int BlastRange
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(attack, "BlastRange");
			set => ModEntry.Instance.Helper.ModData.SetModData(attack, "BlastRange", value);
		}
		
		public int BlastDirection
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(attack, "BlastDirection");
			set => ModEntry.Instance.Helper.ModData.SetModData(attack, "BlastDirection", value);
		}
		
		public int? BlastDamage
		{
			get => ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(attack, "BlastDamage");
			set => ModEntry.Instance.Helper.ModData.SetOptionalModData(attack, "BlastDamage", value);
		}
	}
}

internal sealed class BlastAction : IRegisterable
{
	private static readonly Dictionary<(int direction, bool isShort), ISpriteEntry> Icons = [];

	private static AAttack? AttackContext;
	private static bool IsDuringBlastEffect;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		for (var direction = -1; direction <= 1; direction++)
		{
			var directionString = direction switch
			{
				< 0 => "Left",
				> 0 => "Right",
				_ => "Centered"
			};
			Icons[(direction, true)] = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Actions/Blast{directionString}Short.png"));
			Icons[(direction, false)] = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Actions/Blast{directionString}Wide.png"));
		}
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetIcon)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetIcon_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix_VeryHigh)), priority: Priority.VeryHigh),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer)),
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

	private static void AAttack_GetIcon_Postfix(AAttack __instance, ref Icon? __result)
	{
		if (__instance.BlastRange <= 0)
			return;
		if (__instance.HideBlastInCardRendering)
			return;

		var icon = Icons[(Math.Sign(__instance.BlastDirection), __instance.BlastRange == 1)];
		if (__result is null)
			__result = new(icon.Sprite, __instance.damage, Colors.redd);
		else
			__result = __result.Value with { path = icon.Sprite };
	}

	private static void AAttack_GetTooltips_Postfix(AAttack __instance, ref List<Tooltip> __result)
	{
		if (__instance.BlastRange <= 0)
			return;
		if (__instance.HideBlastInCardRendering)
			return;

		var isShort = __instance.BlastRange == 1;
		var shortOrWideString = isShort ? "Short" : "Wide";
		var directionString = __instance.BlastDirection switch
		{
			< 0 => "Left",
			> 0 => "Right",
			_ => "Centered"
		};
		
		for (var i = 0; i < __result.Count; i++)
		{
			if (__result[i] is not TTGlossary { key: "action.attack.name" })
				continue;
			
			var icon = Icons[(Math.Sign(__instance.BlastDirection), isShort)];
			__result[i] = new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::Blast{directionString}{shortOrWideString}")
			{
				Icon = icon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["Action", "Blast", "Name", directionString, shortOrWideString]),
				Description = ModEntry.Instance.Localizations.Localize(["Action", "Blast", "Description", directionString], new { Damage = __instance.damage, Range = __instance.BlastRange }),
			};
			break;
		}
	}

	private static void AAttack_Begin_Prefix_VeryHigh(AAttack __instance)
	{
		AttackContext = __instance;
		
		if (__instance.BlastRange <= 0)
			return;
		if (__instance.BlastDamage is not null)
			return;
		
		var attackCount = __instance.BlastDirection == 0 ? __instance.BlastRange * 2 + 1 : __instance.BlastRange + 1;
		var splitDamage = __instance.damage / attackCount;
		var leftoverDamage = __instance.damage % attackCount;
			
		__instance.damage = splitDamage + leftoverDamage;
		__instance.BlastDamage = splitDamage;
	}

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;
	
	private static void TriggerBlastIfNeeded(State state, Combat combat, int worldX, bool targetPlayer, bool hitMidrow)
	{
		if (IsDuringBlastEffect)
			return;
		if (AttackContext?.BlastDamage is not { } blastDamage)
			return;

		var targetShip = targetPlayer ? state.ship : combat.otherShip;
		if (!hitMidrow && (targetShip.GetPartAtWorldX(worldX) is not { } part || part.type == PType.empty))
			return;

		AttackContext.timer *= 0.5;
		var effectAction = new EffectAction
		{
			Source = AttackContext,
			TargetPlayer = targetPlayer,
			WorldX = hitMidrow ? worldX : null,
			LocalX = worldX - targetShip.x,
			Damage = blastDamage,
			Range = AttackContext.BlastRange,
			Direction = AttackContext.BlastDirection,
			HitMidrow = hitMidrow,
		};
		ModEntry.Instance.KokoroApi.ActionInfo.SetSourceCardId(effectAction, ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCardId(AttackContext));
		combat.QueueImmediate(effectAction);
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
		if (AttackContext is null)
			return;
		if (IsDuringBlastEffect && AttackContext.CanSecondaryBlastDamageCrit)
			return;
		if (!IsDuringBlastEffect)
		{
			if (AttackContext.BlastRange == 0)
				return;
			if (AttackContext.CanPrimaryBlastDamageCrit)
				return;
		}
		
		if (__result is PDamMod.weak or PDamMod.brittle)
			__result = PDamMod.none;
	}
	
	private sealed class EffectAction : CardAction
	{
		private const double SinglePartDuration = 0.2;

		public AAttack? Source;
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
				AttackContext = Source;
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
				AttackContext = null;
				IsDuringBlastEffect = false;
			}
		}
	}
}