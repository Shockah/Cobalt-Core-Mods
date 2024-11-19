using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class SogginsCatArtifact : Artifact, IRegisterableArtifact, ISmugHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Cat",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Cat.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Cat",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.CatDuoArtifactName.ToUpper(), I18n.CatDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), [(Deck)Instance.SogginsDeck.Id!.Value, Deck.catartifact]);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? [];
		tooltips.Add(Instance.Api.GetSmugTooltip());
		tooltips.Add(new TTGlossary("status.missingCat", 1));
		return tooltips;
	}

	public void OnCardBotchedBySmug(State state, Combat combat, Card card)
	{
		if (state.deck.Concat(combat.hand).Concat(combat.discard).Concat(combat.exhausted).Count(c => c.GetMeta().deck == Deck.colorless) < 7)
			return;

		combat.Queue(new AEnergy
		{
			changeAmount = 1,
			artifactPulse = Key()
		});
		combat.Queue(new AStatus
		{
			status = Status.missingCat,
			statusAmount = 1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}
}
