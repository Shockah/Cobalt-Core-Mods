using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Common })]
public sealed class SogginsPeriArtifact : Artifact, IRegisterableArtifact, ISmugHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Peri",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Peri.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Peri",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.PeriDuoArtifactName.ToUpper(), I18n.PeriDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), new[] { (Deck)Instance.SogginsDeck.Id!.Value, Deck.peri });
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.GetSmugTooltip());
		tooltips.Add(new TTCard { card = Instance.Api.MakePlaceholderApology() });
		return tooltips;
	}

	public void OnCardBotchedBySmug(State state, Combat combat, Card card)
		=> Trigger(state, combat, card);

	public void OnCardDoubledBySmug(State state, Combat combat, Card card)
		=> Trigger(state, combat, card);

	private void Trigger(State state, Combat combat, Card card)
	{
		if (card.GetMeta().deck != Deck.peri)
			return;
		combat.Queue(new AAddCard
		{
			card = Instance.Api.GenerateAndTrackApology(state, combat, state.rngActions),
			destination = CardDestination.Hand
		});
	}
}
