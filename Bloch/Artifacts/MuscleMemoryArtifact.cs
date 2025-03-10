﻿using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MuscleMemoryArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("MuscleMemory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/MuscleMemory.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.AllAssemblies()
				.First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro")
				.GetType("Shockah.Kokoro.OnDiscardManager")!
				.GetNestedType("TriggerAction", AccessTools.all)!
				.GetMethod("Begin", AccessTools.all)!,
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(OnDiscardManager_TriggerAction_Begin_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new ADummyAction()).AsCardAction.GetTooltips(DB.fakeState);

	private static void OnDiscardManager_TriggerAction_Begin_Postfix(CardAction __instance, State s, Combat c)
	{
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is MuscleMemoryArtifact) is not { } artifact)
			return;
		if (ModEntry.Instance.KokoroApi.OnDiscard.AsAction(__instance) is not { } onDiscardAction)
			return;
		
		if (string.IsNullOrEmpty(onDiscardAction.Action.artifactPulse))
			onDiscardAction.Action.artifactPulse = artifact.Key();
		c.QueueImmediate(onDiscardAction.Action);
	}
}