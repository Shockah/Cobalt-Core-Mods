using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class SogginsRiggsArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Riggs",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Riggs.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Riggs",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.RiggsDuoArtifactName.ToUpper(), I18n.RiggsDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), [(Deck)Instance.SogginsDeck.Id!.Value, Deck.riggs]);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? [];
		tooltips.Add(new TTGlossary("status.evade"));
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;
		if (state.ship.Get(Status.evade) >= 2)
			return;

		combat.Queue(new AStatus
		{
			status = Status.evade,
			statusAmount = 1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	public override void OnPlayerTakeNormalDamage(State state, Combat combat, int rawAmount, Part? part)
	{
		base.OnPlayerTakeNormalDamage(state, combat, rawAmount, part);
		combat.Queue(new AStatus
		{
			status = (Status)Instance.SmugStatus.Id!.Value,
			statusAmount = -1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}
}
