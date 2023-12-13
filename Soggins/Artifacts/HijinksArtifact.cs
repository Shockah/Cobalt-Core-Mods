using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Boss })]
public sealed class HijinksArtifact : Artifact, IRegisterableArtifact, ISmugHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Hijinks",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "HijinksArtifact.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Hijinks",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.HijinksArtifactName.ToUpper(), I18n.HijinksArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public override void OnReceiveArtifact(State state)
		=> state.ship.baseEnergy++;

	public override void OnRemoveArtifact(State state)
		=> state.ship.baseEnergy--;

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue(new AStatus
		{
			status = (Status)Instance.FrogproofingStatus.Id!.Value,
			statusAmount = 3,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	public double ModifySmugBotchChance(State state, Ship ship, double chance)
		=> chance + 0.05;
}
