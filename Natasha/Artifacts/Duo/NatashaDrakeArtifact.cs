using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Natasha;

internal sealed class NatashaDrakeArtifact : Artifact, IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("NatashaDrake", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Drake.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Drake", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Drake", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.NatashaDeck.Deck, Deck.eunice]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("action.attackPiercing"),
			new TTGlossary("action.stun"),
			.. StatusMeta.GetTooltips(Status.lockdown, 1),
		];

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0).ExtractLabels(out var labels),
					ILMatches.Ldflda("status"),
					ILMatches.Call("get_HasValue"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler_ApplyExtraStatus))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void AAttack_Begin_Transpiler_ApplyExtraStatus(AAttack attack, State state, Combat combat)
	{
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is NatashaDrakeArtifact) is not { } artifact)
			return;
		if (attack is { piercing: false, stunEnemy: false })
			return;
		
		combat.QueueImmediate(new AStatus { targetPlayer = attack.targetPlayer, status = Status.lockdown, statusAmount = 1, artifactPulse = artifact.Key() });
	}
}