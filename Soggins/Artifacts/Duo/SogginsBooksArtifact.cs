using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class SogginsBooksArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Books",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Books.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Books",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.BooksDuoArtifactName.ToUpper(), I18n.BooksDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), new[] { (Deck)Instance.SogginsDeck.Id!.Value, Deck.shard });
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var maxShardAmount = (StateExt.Instance ?? DB.fakeState).ship.Get(Status.maxShard);
		if (maxShardAmount == 0)
			maxShardAmount = 3;

		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTGlossary($"status.{Status.shard.Key()}", maxShardAmount));
		tooltips.Add(new TTCard { card = Instance.Api.MakePlaceholderApology() });
		return tooltips;
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;

		int count = Instance.KokoroApi.GetCardsEverywhere(state, hand: false, exhaustPile: false)
			.Where(c => c is ApologyCard)
			.Count();

		for (int i = 0; i < count / 2; i++)
			combat.Queue(new AStatus
			{
				status = Status.shard,
				statusAmount = 1,
				targetPlayer = true,
				artifactPulse = Key()
			});
	}
}
