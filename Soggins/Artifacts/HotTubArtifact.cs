using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Common })]
public sealed class HotTubArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.HotTub",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "HotTub.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.HotTub",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.HotTubArtifactName.ToUpper(), I18n.HotTubArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(GetLocDesc)),
			postfix: new HarmonyMethod(GetType(), nameof(Artifact_GetLocDesc_Postfix))
		);
	}

	public override string Description()
	{
		if (StateExt.Instance is not { } state)
			return base.Description();
		return string.Format(I18n.HotTubArtifactDescription, Instance.Api.GetMinSmug(state.ship), Instance.Api.GetMaxSmug(state.ship));
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue(new AStatus
		{
			status = (Status)Instance.FrogproofingStatus.Id!.Value,
			statusAmount = 3,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.turn == 0)
			return;
		var smug = Instance.Api.GetSmug(state.ship);
		if (smug is null)
			return;

		if (smug == Instance.Api.GetMinSmug(state.ship))
			combat.Queue(new AStatus
			{
				status = (Status)Instance.Api.SmugStatus.Id!.Value,
				statusAmount = 1,
				targetPlayer = true,
				artifactPulse = Key()
			});
		else if (smug == Instance.Api.GetMaxSmug(state.ship))
			combat.Queue(new AStatus
			{
				status = (Status)Instance.Api.SmugStatus.Id!.Value,
				statusAmount = -1,
				targetPlayer = true,
				artifactPulse = Key()
			});
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	private static void Artifact_GetLocDesc_Postfix(Artifact __instance, ref string __result)
	{
		if (__instance is not HotTubArtifact)
			return;
		__result = __instance.Description();
	}
}
