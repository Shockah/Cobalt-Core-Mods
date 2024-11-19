using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Dyna;

internal static class BlastwaveExt
{
	public static bool IsBlastwave(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(self, "IsBlastwave");

	public static bool IsStunwave(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(self, "IsStunwave");

	public static int? GetBlastwaveDamage(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(self, "BlastwaveDamage");

	public static int GetBlastwaveRange(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault(self, "BlastwaveRange", 1);

	public static AAttack SetBlastwave(this AAttack self, int? damage, int range = 1, bool isStunwave = false)
	{
		ModEntry.Instance.Helper.ModData.SetModData(self, "IsBlastwave", true);
		ModEntry.Instance.Helper.ModData.SetModData(self, "IsStunwave", isStunwave);
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "BlastwaveDamage", damage);
		ModEntry.Instance.Helper.ModData.SetModData(self, "BlastwaveRange", range);
		return self;
	}
}

internal sealed class BlastwaveManager
{
	private static ISpriteEntry BlastwaveIcon = null!;
	private static ISpriteEntry WideBlastwaveIcon = null!;
	private static ISpriteEntry StunBlastwaveIcon = null!;

	private static AAttack? AttackContext;

	public BlastwaveManager()
	{
		BlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/Blastwave.png"));
		WideBlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/BlastwaveWide.png"));
		StunBlastwaveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Icons/BlastwaveStun.png"));

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Transpiler)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetTooltips)),
			postfix: new HarmonyMethod(GetType(), nameof(AAttack_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix))
		);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerTakeNormalDamage), (State state, Combat combat, Part? part) =>
		{
			if (part is null)
				return;
			var worldX = state.ship.x + state.ship.parts.IndexOf(part);
			TriggerBlastwaveIfNeeded(state, combat, worldX, targetPlayer: true, hitMidrow: false);
		}, 0);

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnEnemyGetHit), (State state, Combat combat, Part? part) =>
		{
			if (part is null)
				return;
			var worldX = combat.otherShip.x + combat.otherShip.parts.IndexOf(part);
			TriggerBlastwaveIfNeeded(state, combat, worldX, targetPlayer: false, hitMidrow: false);
		}, 0);
	}

	private static void TriggerBlastwaveIfNeeded(State state, Combat combat, int worldX, bool targetPlayer, bool hitMidrow)
	{
		if (AttackContext is not { } attack)
			return;
		if (!attack.IsBlastwave())
			return;

		if (!hitMidrow)
		{
			var targetShip = targetPlayer ? state.ship : combat.otherShip;
			if (targetShip.GetPartAtWorldX(worldX) is null)
				return;
		}

		attack.timer *= 0.5;
		combat.QueueImmediate(new BlastwaveAction
		{
			Source = attack,
			TargetPlayer = targetPlayer,
			WorldX = worldX,
			Damage = attack.GetBlastwaveDamage(),
			Range = attack.GetBlastwaveRange(),
			IsStunwave = attack.IsStunwave(),
			HitMidrow = hitMidrow,
		});
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<RaycastResult>(originalMethod).CreateLdlocInstruction(out var ldlocRaycastResult),
					ILMatches.Ldfld("hitDrone"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				)
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_2).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_3),
					ldlocRaycastResult,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler_AfterDroneHitCheck)))
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

	private static void AAttack_Begin_Transpiler_AfterDroneHitCheck(State state, Combat combat, RaycastResult raycastResult)
	{
		if (raycastResult.fromDrone || raycastResult.hitShip || !raycastResult.hitDrone)
			return;
		if (AttackContext is not { } attack)
			return;
		TriggerBlastwaveIfNeeded(state, combat, raycastResult.worldX, attack.targetPlayer, hitMidrow: true);
	}

	private static void AAttack_GetTooltips_Postfix(AAttack __instance, State s, ref List<Tooltip> __result)
	{
		if (!__instance.IsBlastwave())
			return;

		__result.AddRange(new BlastwaveAction
		{
			Source = __instance,
			TargetPlayer = __instance.targetPlayer,
			WorldX = 0,
			Damage = __instance.GetBlastwaveDamage(),
			Range = __instance.GetBlastwaveRange(),
			IsStunwave = __instance.IsStunwave(),
		}.GetTooltips(s));
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AAttack attack)
			return true;
		if (!attack.IsBlastwave())
			return true;

		var copy = Mutil.DeepCopy(attack);
		ModEntry.Instance.Helper.ModData.SetModData(copy, "IsBlastwave", false);

		var position = g.Push(rect: new()).rect.xy;
		int initialX = (int)position.x;

		position.x += Card.RenderAction(g, state, copy, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		__result += 2;

		if (!dontDraw)
		{
			ISpriteEntry icon;
			if (attack.IsStunwave())
				icon = StunBlastwaveIcon;
			else if (attack.GetBlastwaveRange() >= 2)
				icon = WideBlastwaveIcon;
			else
				icon = BlastwaveIcon;

			Draw.Sprite(icon.Sprite, initialX + __result, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		}
		__result += 9;

		if (attack.GetBlastwaveDamage() is { } damage)
		{
			__result++;
			if (!dontDraw)
				BigNumbers.Render(damage, initialX + __result, position.y, action.disabled ? Colors.disabledText : Colors.redd);
			__result += damage.ToString().Length * 6;
		}

		return false;
	}

	internal sealed class BlastwaveAction : CardAction
	{
		private const double SinglePartDuration = 0.2;

		public required AAttack Source;
		public bool TargetPlayer;
		public required int WorldX;
		public required int? Damage;
		public int Range = 1;
		public bool IsStunwave;
		public bool IsPiercing;
		public bool HitMidrow;

		public override List<Tooltip> GetTooltips(State s)
		{
			string key;
			string name;
			string description;
			ISpriteEntry icon;

			if (IsStunwave)
			{
				icon = StunBlastwaveIcon;
				name = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "name", "Stun"]);
				if (Damage is { } damage)
				{
					key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Stun::StunDamage";
					description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "StunDamage"], new { Range, Damage = damage });
				}
				else
				{
					key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Stun::Stun";
					description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "Stun"], new { Range });
				}
			}
			else if (Range >= 2)
			{
				icon = WideBlastwaveIcon;
				key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Wide::Damage";
				name = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "name", "Wide"]);
				description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "Damage"], new { Range, Damage = Damage ?? 0 });
			}
			else
			{
				icon = BlastwaveIcon;
				key = $"{ModEntry.Instance.Package.Manifest.UniqueName}::Blastwave::Normal::Damage";
				name = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "name", "Normal"]);
				description = ModEntry.Instance.Localizations.Localize(["action", "Blastwave", "description", "Damage"], new { Range, Damage = Damage ?? 0 });
			}

			List<Tooltip> tooltips = [
				new GlossaryTooltip(key)
				{
					Icon = icon.Sprite,
					TitleColor = Colors.action,
					Title = name,
					Description = description
				}
			];

			if (IsStunwave)
				tooltips.Add(new TTGlossary("action.stun"));

			return tooltips;
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = Range * SinglePartDuration;

			if (Range > 0)
				Run(g, s, c, 1);

			var targetShip = TargetPlayer ? s.ship : c.otherShip;
			foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, s.EnumerateAllArtifacts()))
				hook.OnBlastwaveTrigger(s, c, targetShip, WorldX, HitMidrow);
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
			var targetShip = TargetPlayer ? state.ship : combat.otherShip;

			RunAt(WorldX - offset);
			RunAt(WorldX + offset);

			void RunForPartAt(int worldX)
			{
				if (targetShip.GetPartAtWorldX(worldX) is not { } part || part.type == PType.empty)
					return;

				var hitShield = targetShip.Get(Status.shield) + targetShip.Get(Status.tempShield) > 0;
				var damageDone = new DamageDone
				{
					hitShield = hitShield,
					hitHull = !hitShield
				};

				if (IsStunwave || part.stunModifier == PStunMod.stunnable)
					new AStunPart { worldX = worldX }.FullyRun(g, state, combat);

				if (Damage is { } damage)
					damageDone = targetShip.NormalDamage(state, combat, damage, worldX, piercing: IsPiercing);
				else if (IsStunwave)
					ChargeManager.TriggerChargeIfAny(state, combat, part, TargetPlayer);

				var raycastResult = new RaycastResult
				{
					hitShip = true,
					worldX = worldX
				};
				EffectSpawnerExt.HitEffect(g, TargetPlayer, raycastResult, damageDone);

				if (!TargetPlayer)
				{
					combat.otherShip.ai?.OnHitByAttack(state, combat, worldX, Source);
					foreach (var artifact in state.EnumerateAllArtifacts())
						artifact.OnEnemyGetHit(state, combat, part);
				}

				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
					hook.OnBlastwaveHit(state, combat, targetShip, WorldX, worldX, HitMidrow);
			}

			void RunForMidrowAt(int worldX)
			{
				if (!combat.stuff.TryGetValue(worldX, out var @object))
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
					worldX = worldX
				};
				EffectSpawnerExt.HitEffect(g, TargetPlayer, raycastResult, damageDone);

				if (@object.bubbleShield)
				{
					@object.bubbleShield = false;
					Audio.Play(Event.Hits_ShieldPop);
				}
				else if (isInvincible)
				{
					combat.QueueImmediate(@object.GetActionsOnShotWhileInvincible(state, combat, !TargetPlayer, Damage ?? 0));
				}
				else
				{
					combat.DestroyDroneAt(state, worldX, !TargetPlayer);
				}

				foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
					hook.OnBlastwaveHit(state, combat, targetShip, worldX, WorldX, HitMidrow);
			}

			void RunAt(int worldX)
			{
				if (HitMidrow)
					RunForMidrowAt(worldX);
				else
					RunForPartAt(worldX);
			}
		}
	}
}

internal sealed class BlastwaveFakeStuff : StuffBase
{
}