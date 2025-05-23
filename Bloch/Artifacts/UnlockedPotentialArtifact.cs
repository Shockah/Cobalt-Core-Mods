﻿using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class UnlockedPotentialArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("UnlockedPotential", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/UnlockedPotential.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnlockedPotential", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnlockedPotential", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.AllAssemblies()
				.First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro")
				.GetType("Shockah.Kokoro.SpontaneousManager")!
				.GetNestedType("TriggerAction", AccessTools.all)!
				.GetMethod("Begin", AccessTools.all)!,
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SpontaneousManager_TriggerAction_Begin_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> ModEntry.Instance.KokoroApi.Impulsive.MakeAction(new ADummyAction()).AsCardAction.GetTooltips(DB.fakeState);

	private static void SpontaneousManager_TriggerAction_Begin_Postfix(CardAction __instance, State s, Combat c)
	{
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is UnlockedPotentialArtifact) is not { } artifact)
			return;
		if (ModEntry.Instance.KokoroApi.Impulsive.AsAction(__instance) is not { } impulsiveAction)
			return;

		if (string.IsNullOrEmpty(impulsiveAction.Action.artifactPulse))
			impulsiveAction.Action.artifactPulse = artifact.Key();
		c.QueueImmediate(impulsiveAction.Action);
	}
}