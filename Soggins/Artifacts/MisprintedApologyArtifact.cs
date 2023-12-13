using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Common })]
public sealed class MisprintedApologyArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;
	private static ExternalSprite InactiveSprite = null!;

	public bool TriggeredThisTurn = false;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.MisprintedApology",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "MisprintedApologyArtifact.png"))
		);
		InactiveSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.MisprintedApologyInactive",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "MisprintedApologyInactiveArtifact.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.MisprintedApology",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.MisprintedApologyArtifactName.ToUpper(), I18n.MisprintedApologyArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public override Spr GetSprite()
		=> (Spr)(TriggeredThisTurn ? InactiveSprite : Sprite).Id!.Value;

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTCard { card = new RandomPlaceholderApologyCard() });
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisTurn = false;
	}

	public override void OnCombatEnd(State state)
	{
		base.OnCombatEnd(state);
		TriggeredThisTurn = false;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		TriggeredThisTurn = false;
	}
}
