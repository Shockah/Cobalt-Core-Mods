using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class SogginsDizzyArtifact : Artifact, IRegisterableArtifact, ISmugHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Dizzy",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Dizzy.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Dizzy",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.DizzyDuoArtifactName.ToUpper(), I18n.DizzyDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), new[] { (Deck)Instance.SogginsDeck.Id!.Value, Deck.dizzy });
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.GetSmugTooltip());
		tooltips.Add(new TTGlossary("status.shieldAlt"));
		return tooltips;
	}

	public double ModifySmugBotchChance(State state, Ship ship, Card? card, double chance)
	{
		if (ship != state.ship)
			return chance;
		return chance + (ship.GetMaxShield() - ship.Get(Status.shield)) * 0.03;
	}

	public double ModifySmugDoubleChance(State state, Ship ship, Card? card, double chance)
	{
		if (ship != state.ship)
			return chance;
		return chance + ship.Get(Status.shield) * 0.03;
	}
}
