using System;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace Shockah.MORE;

internal sealed class PatchNotesArtifact : Artifact, IRegisterable
{
	private static ACardOffering? CardOfferingActionContext;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("PatchNotes", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.catartifact,
				pools = [ArtifactPool.Common],
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifact/CAT/PatchNotes.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "CAT", "PatchNotes", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "CAT", "PatchNotes", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardOffering), nameof(ACardOffering.BeginWithRoute)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetUpgrade)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetUpgrade_Transpiler))
		);
	}

	private static void Card_GetActionsOverridden_Postfix(Card __instance, State s, ref List<CardAction> __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is PatchNotesArtifact))
			return;
		if (!ModEntry.Instance.EssentialsApi.IsExeCardType(__instance.GetType()))
			return;

		foreach (var action in __result)
		{
			if (action is not ACardOffering cardOfferingAction)
				continue;
			cardOfferingAction.overrideUpgradeChances = null;
			ModEntry.Instance.Helper.ModData.SetModData(cardOfferingAction, "OverrideUpgradeChance", 0.5);
		}
	}

	private static void ACardOffering_BeginWithRoute_Prefix(ACardOffering __instance)
		=> CardOfferingActionContext = __instance;

	private static void ACardOffering_BeginWithRoute_Finalizer()
		=> CardOfferingActionContext = null;
	
	private static IEnumerable<CodeInstruction> CardReward_GetUpgrade_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(2),
					ILMatches.Call("GetUpgradeChance"),
					ILMatches.Ldarg(4),
					ILMatches.Instruction(OpCodes.Mul),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetUpgrade_Transpiler_ModifyChance)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static double CardReward_GetUpgrade_Transpiler_ModifyChance(double chance)
	{
		if (CardOfferingActionContext is null)
			return chance;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData(CardOfferingActionContext, "OverrideUpgradeChance", out double overrideUpgradeChance))
			return chance;
		return overrideUpgradeChance;
	}
}