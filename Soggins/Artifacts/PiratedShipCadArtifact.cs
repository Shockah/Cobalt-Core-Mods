using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

[ArtifactMeta(unremovable = true)]
internal sealed class PiratedShipCadArtifact : Artifact, IRegisterableArtifact, ISmugHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private static ExternalSprite Sprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		Sprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.PiratedShipCad",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "PiratedShipCadArtifact.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.PiratedShipCad",
			artifactType: GetType(),
			sprite: Sprite,
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.PiratedShipCadArtifactName.ToUpper(), I18n.PiratedShipCadArtifactDescription);
		registry.RegisterArtifact(artifact);

		Instance.Api.RegisterSmugHook(this, 0);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(Instance.Api.GetSmugTooltip());
		tooltips.Add(new TTGlossary("status.tempShieldAlt"));
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Instance.Api.SetSmug(state.ship, 0);
	}

	public void OnCardBotchedBySmug(State state, Combat combat, Card card)
	{
		var artifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is PiratedShipCadArtifact);
		if (artifact is null)
			return;

		combat.Queue(new AStatus
		{
			status = Status.tempShield,
			statusAmount = 1,
			targetPlayer = true,
			artifactPulse = artifact.Key()
		});
		artifact.Pulse();
	}
}
