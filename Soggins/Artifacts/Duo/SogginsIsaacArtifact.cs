using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class SogginsIsaacArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Isaac",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Isaac.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Isaac",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.IsaacDuoArtifactName.ToUpper(), I18n.IsaacDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), [(Deck)Instance.SogginsDeck.Id!.Value, Deck.goat]);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? [];
		tooltips.Add(new TTGlossary("action.spawn"));
		tooltips.Add(new TTGlossary("midrow.drone", 1));
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.turn == 0)
			return;
		if (combat.energy <= 0)
			return;

		var smugResult = Instance.Api.RollSmugResult(state, state.ship, state.rngActions, null);
		Pulse();
		if (smugResult != SmugResult.Normal)
			state.ship.PulseStatus((Status)Instance.Api.SmugStatus.Id!.Value);

		combat.Queue(new ASpawn
		{
			thing = new AttackDrone
			{
				targetPlayer = smugResult == SmugResult.Botch,
				upgraded = smugResult == SmugResult.Double
			},
			artifactPulse = Key()
		});
	}
}