using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Common })]
public sealed class VideoWillArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.VideoWill",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "VideoWillArtifact.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.VideoWill",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.VideoWillArtifactName.ToUpper(), I18n.VideoWillArtifactDescription);
		registry.RegisterArtifact(artifact);
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

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.FrogproofingTooltip);
		return tooltips;
	}
}
