using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Boss })]
public sealed class RepeatedMistakesArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.RepeatedMistakes",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "RepeatedMistakes.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.RepeatedMistakes",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.RepeatedMistakesArtifactName.ToUpper(), I18n.RepeatedMistakesArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue(new AStatus
		{
			status = Status.backwardsMissiles,
			statusAmount = 3,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;

		combat.QueueImmediate(new ASpawn
		{
			thing = new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.seeker
			},
			artifactPulse = Key()
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTGlossary("status.backwardsMissiles"));
		tooltips.Add(new TTGlossary("action.spawn"));
		tooltips.Add(new TTGlossary("midrow.missile_seeker", 2));
		return tooltips;
	}
}
