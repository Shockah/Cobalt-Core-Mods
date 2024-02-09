using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Soggins;

[ArtifactMeta(unremovable = true)]
public sealed class SmugArtifact : Artifact, IRegisterableArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly ExternalSprite[] Sprites = new ExternalSprite[7];
	private static ExternalSprite OversmugSprite = null!;

	public void RegisterArt(ISpriteRegistry registry)
	{
		for (int smug = -Sprites.Length / 2; smug <= Sprites.Length / 2; smug++)
			Sprites[smug + Sprites.Length / 2] = registry.RegisterArtOrThrow(
				id: $"{GetType().Namespace}.Artifact.Smug{smug}",
				file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Smug", $"{smug}.png"))
			);
		OversmugSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.SmugOversmug",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Smug", $"Oversmug.png"))
		);
	}

	public void RegisterArtifact(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Smug",
			artifactType: GetType(),
			sprite: Sprites[Sprites.Length / 2],
			ownerDeck: Instance.SogginsDeck
		);
		artifact.AddLocalisation(I18n.SmugArtifactName.ToUpper(), I18n.SmugArtifactDescription);
		registry.RegisterArtifact(artifact);
	}

	public override Spr GetSprite()
	{
		var state = StateExt.Instance ?? DB.fakeState;
		if (state.route is not Combat)
			return base.GetSprite();
		var smug = Instance.Api.GetSmug(state, state.ship);
		if (smug is null)
			return base.GetSprite();

		if (Instance.Api.IsOversmug(state, state.ship))
			return (Spr)OversmugSprite.Id!.Value;
		var spriteIndex = Math.Clamp(smug.Value + Sprites.Length / 2, 0, Sprites.Length - 1);
		return (Spr)Sprites[spriteIndex].Id!.Value;
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? [];
		tooltips.Add(Instance.Api.GetSmugTooltip());
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue(new AEnableSmug()
		{
			artifactPulse = Key(),
			statusPulse = (Status)Instance.SmugStatus.Id!.Value
		});
	}
}
