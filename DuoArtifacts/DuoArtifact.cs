using HarmonyLib;
using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Boss })]
public abstract class DuoArtifact : Artifact
{
	protected internal virtual void ApplyPatches(Harmony harmony)
	{
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips();
		var definition = DuoArtifactDefinition.GetDefinition(GetType());
		if (definition is null || definition.ExtraGlossary.Count == 0)
			return tooltips;

		tooltips ??= new();
		foreach (var glossaryKey in definition.ExtraGlossary)
			tooltips.Add(new TTGlossary(glossaryKey));
		return tooltips;
	}
}
