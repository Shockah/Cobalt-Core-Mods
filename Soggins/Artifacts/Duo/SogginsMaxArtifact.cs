using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class SogginsMaxArtifact : Artifact, IRegisterableArtifact, ISmugHook, IHookPriority
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	private HashSet<Card> ModifiedCards = new();

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Max",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Max.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Max",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.MaxDuoArtifactName.ToUpper(), I18n.MaxDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), new[] { (Deck)Instance.SogginsDeck.Id!.Value, Deck.hacker });
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	public double HookPriority
		=> -100;

	public override void OnCombatEnd(State state)
	{
		base.OnCombatEnd(state);
		ModifiedCards.Clear();
	}

	public double ModifySmugBotchChance(State state, Ship ship, Card? card, double chance)
		=> ModifySmugChance(state, card, chance);

	public double ModifySmugDoubleChance(State state, Ship ship, Card? card, double chance)
		=> ModifySmugChance(state, card, chance);

	private double ModifySmugChance(State state, Card? card, double chance)
	{
		if (card is null || state.route is not Combat combat || combat.hand.Count < 2)
			return chance;

		int handPosition = combat.hand.IndexOf(card);
		if (handPosition != 0 && handPosition != combat.hand.Count - 1)
			return chance;

		ModifiedCards.Add(card);
		return chance * 2;
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (ModifiedCards.Contains(card))
			Pulse();
	}
}
