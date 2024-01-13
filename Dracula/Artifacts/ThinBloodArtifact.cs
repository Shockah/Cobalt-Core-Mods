using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class ThinBloodArtifact : Artifact, IDraculaArtifact
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("ThinBlood", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/ThinBlood.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ThinBlood", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ThinBlood", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(ModEntry.Instance.BleedingStatus.Status, 1)
			.Concat(StatusMeta.GetTooltips(ModEntry.Instance.TransfusionStatus.Status, 1))
			.Concat(StatusMeta.GetTooltips(ModEntry.Instance.TransfusingStatus.Status, 0))
			.ToList();
}