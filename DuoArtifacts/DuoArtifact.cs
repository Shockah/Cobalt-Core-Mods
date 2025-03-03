using CobaltCoreModding.Definitions.ModContactPoints;
using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

public abstract class DuoArtifact : Artifact
{
	protected static ModEntry Instance => ModEntry.Instance;

	protected internal virtual void ApplyPatches(IHarmony harmony)
	{
	}

	protected internal virtual void ApplyLatePatches(IHarmony harmony)
	{
	}

	protected internal virtual void RegisterArt(ISpriteRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
	}

	protected internal virtual void RegisterCards(ICardRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			.. base.GetExtraTooltips() ?? [],
			.. DuoArtifactDefinition.GetDefinition(GetType()) is { } definition ? definition.ExtraTooltips.Select(tooltip => tooltip.MakeTooltip()) : [],
		];
}
