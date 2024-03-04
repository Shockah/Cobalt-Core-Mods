using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaDizzyArtifact : Artifact, IDraculaArtifact
{
	internal const int ResultingOxidation = 2;

	public static void Register(IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			throw new InvalidOperationException();

		helper.Content.Artifacts.RegisterArtifact("DraculaDizzy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaDizzy.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDizzy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDizzy", "description"], new { ResultingOxidation }).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DraculaDeck.Deck, Deck.dizzy]);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(ModEntry.Instance.BleedingStatus.Status, 1)
			.Concat(StatusMeta.GetTooltips(ModEntry.Instance.OxidationStatus.Status, ResultingOxidation))
			.Concat(StatusMeta.GetTooltips(Status.corrode, 1))
			.ToList();
}