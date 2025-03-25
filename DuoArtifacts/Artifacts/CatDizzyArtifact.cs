using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json;

namespace Shockah.DuoArtifacts;

internal sealed class CatDizzyArtifact : DuoArtifact
{
	private static ExternalSprite InactiveSprite { get; set; } = null!;

	[JsonProperty]
	private bool TriggeredThisCombat;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			transpiler: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Prefix))
		);
	}

	protected internal override void RegisterArt(ISpriteRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterArt(registry, namePrefix, definition);
		InactiveSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}.Inactive",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifacts", "CatDizzyInactive.png"))
		);
	}

	public override Spr GetSprite()
		=> TriggeredThisCombat ? (Spr)InactiveSprite.Id!.Value : base.GetSprite();

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	private static void Trigger(State state)
	{
		if (state.EnumerateAllArtifacts().OfType<CatDizzyArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisCombat)
			return;

		artifact.Pulse();
		state.ship.Add(Status.perfectShield, state.ship.Get(Status.shield));
		state.ship.Set(Status.shield, 0);
		state.ship.Set(Status.maxShield, Math.Max(-state.ship.shieldMaxBase + 1, state.ship.Get(Status.maxShield)));
		artifact.TriggeredThisCombat = true;
	}

	private static IEnumerable<CodeInstruction> Ship_NormalDamage_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(3),
					ILMatches.Stloc<int>(originalMethod).CreateLdlocInstruction(out var ldlocRemainingDamage)
				])
				.Find([
					ILMatches.Ldarg(5),
					ILMatches.Brtrue
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocRemainingDamage,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CatDizzyArtifact), nameof(Ship_NormalDamage_Transpiler_ApplyPerfectShieldIfNeeded)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Ship_NormalDamage_Transpiler_ApplyPerfectShieldIfNeeded(Ship ship, State state, int remainingDamage)
	{
		if (ship != state.ship)
			return;
		if (remainingDamage <= ship.Get(Status.tempShield))
			return;
		Trigger(state);
	}

	private static void Ship_DirectHullDamage_Prefix(Ship __instance, State s, int amt)
	{
		if (__instance != s.ship)
			return;
		if (amt <= 0)
			return;
		Trigger(s);
	}
}