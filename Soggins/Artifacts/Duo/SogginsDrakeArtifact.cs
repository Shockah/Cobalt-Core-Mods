using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = [ArtifactPool.Common])]
public sealed class SogginsDrakeArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;
	private static ExternalSprite InactiveSprite = null!;

	public bool TriggeredThisCombat = false;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.Drake",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "Drake.png"))
		);
		InactiveSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Duo.DrakeInactive",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Artifact", "Duo", "DrakeInactive.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Duo.Drake",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.DuoArtifactsApi!.DuoArtifactDeck
		);
		artifact.AddLocalisation(I18n.DrakeDuoArtifactName.ToUpper(), I18n.DrakeDuoArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public void ApplyPatches(Harmony harmony)
	{
		Instance.DuoArtifactsApi!.RegisterDuoArtifact(GetType(), new[] { (Deck)Instance.SogginsDeck.Id!.Value, Deck.eunice });
	}

	public override Spr GetSprite()
		=> (Spr)(TriggeredThisCombat ? InactiveSprite : Sprite).Id!.Value;

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTGlossary("action.overheat"));
		tooltips.Add(new TTGlossary($"status.{Instance.ConstantApologiesStatus.Id!.Value}", 1));
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	public override void AfterPlayerOverheat(State state, Combat combat)
	{
		base.AfterPlayerOverheat(state, combat);
		if (TriggeredThisCombat)
			return;

		combat.Queue(new AStatus
		{
			status = (Status)Instance.ConstantApologiesStatus.Id!.Value,
			statusAmount = 1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}
}
