using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using FMOD;
using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using ILMatches = Nanoray.Shrike.Harmony.ILMatches;

namespace Shockah.NewLight;

internal sealed class Jolted : IRegisterable
{
	public static CustomPartTraits.ITraitEntry Trait { get; private set; } = null!;
	private static ISpriteEntry Icon = null!;

	private static AAttack? AttackContext;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/PartTraits/Jolted.png"));
		
		Trait = CustomPartTraits.RegisterTrait(package.Manifest, "Jolted", new()
		{
			Icon = _ => Icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["PartTrait", "Jolted", "Name"]).Localize,
			Tooltips = _ => GetTooltips(true),
		});
		
		CustomPartTraits.Instance.Register(new CustomPartTraitsHook(), 0);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix))
		);
	}

	public static IEnumerable<Tooltip> GetTooltips(bool appliedToPart)
		=> [
			new GlossaryTooltip($"parttrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Jolted")
			{
				Icon = Icon.Sprite,
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["PartTrait", "Jolted", "Name"]),
				Description = ModEntry.Instance.Localizations.Localize(["PartTrait", "Jolted", appliedToPart ? "Description" : "FullDescription"]),
			}
		];

	private static void Ship_OnBeginTurn_Postfix(Ship __instance, Combat c)
	{
		foreach (var part in __instance.parts)
			part.Jolted = false;
	}
	
	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

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
		if (AttackContext?.Jolt != true)
			return;
		
		var offset = state.rngActions.NextInt() % 2 == 0 ? -1 : 1;
		combat.QueueImmediate(new JoltMidrowAction { FromPlayer = !AttackContext.targetPlayer, WorldX = raycastResult.worldX + offset });
	}

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s, Combat c, int? maybeWorldGridX)
	{
		if (AttackContext is null)
			return;
		if (__instance == s.ship)
			return;
		if (maybeWorldGridX is not { } worldGridX)
			return;
		if (__instance.GetPartAtWorldX(worldGridX) is not { } part)
			return;

		if (part.Jolted)
		{
			var offset = s.rngActions.NextInt() % 2 == 0 ? -1 : 1;
			c.QueueImmediate(new JoltDamageAction { TargetPlayer = __instance.isPlayerShip, LocalX = worldGridX - __instance.x + offset });
		}
		else if (AttackContext.Jolt)
		{
			part.Jolted = true;
		}
	}

	private sealed class JoltMidrowAction : CardAction
	{
		public required bool FromPlayer;
		public int WorldX;
		public int Damage = 1;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			
			if (!c.stuff.TryGetValue(WorldX, out var @object))
				return;

			var isInvincible = @object.Invincible();
			foreach (var artifact in s.EnumerateAllArtifacts())
			{
				if (artifact.ModifyDroneInvincibility(s, c, @object) == true)
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
				worldX = WorldX
			};
			EffectSpawnerExt.HitEffect(g, !FromPlayer, raycastResult, damageDone);

			if (@object.bubbleShield)
			{
				@object.bubbleShield = false;
				Audio.Play(Event.Hits_ShieldPop);
			}
			else if (isInvincible)
			{
				c.QueueImmediate(@object.GetActionsOnShotWhileInvincible(s, c, FromPlayer, Damage));
			}
			else
			{
				c.DestroyDroneAt(s, WorldX, FromPlayer);
			}
		}
	}

	private sealed class JoltDamageAction : CardAction
	{
		public bool TargetPlayer;
		public int LocalX;
		public int Damage = 1;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var target = TargetPlayer ? s.ship : c.otherShip;

			if (target.GetPartAtLocalX(LocalX) is not { } part || part.type == PType.empty)
			{
				timer = 0;
				return;
			}

			var dmg = target.NormalDamage(s, c, Damage, target.x + LocalX);
			
			GUID? sound = null;
			if (dmg.poppedShield)
				sound = Event.Hits_ShieldPop;
			else if (dmg.hitShield)
				sound = Event.Hits_ShieldHit;
			if (dmg.hitHull)
				sound = TargetPlayer ? Event.Hits_HitHurt : Event.Hits_OutgoingHit;
			if (sound is not null)
				Audio.Play(sound.Value);
		}
	}

	private sealed class CustomPartTraitsHook : CustomPartTraits.IHook
	{
		public void ModifyCustomPartTraits(CustomPartTraits.IHook.ModifyCustomPartTraitsArgs args)
		{
			if (!args.Part.Jolted)
				return;
			args.Traits.Add(Trait);
		}
	}
}

public static class JoltedExt
{
	extension(AAttack attack)
	{
		public bool Jolt
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(attack, "Jolt");
			set
			{
				if (value)
					ModEntry.Instance.Helper.ModData.SetModData(attack, "Jolt", true);
				else
					ModEntry.Instance.Helper.ModData.RemoveModData(attack, "Jolt");
			}
		}
	}
	
	extension(Part part)
	{
		public bool Jolted
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(part, "Jolted");
			set
			{
				if (value)
					ModEntry.Instance.Helper.ModData.SetModData(part, "Jolted", true);
				else
					ModEntry.Instance.Helper.ModData.RemoveModData(part, "Jolted");
			}
		}
	}
}