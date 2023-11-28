using HarmonyLib;
using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

public abstract class DuoArtifact : Artifact
{
	protected internal virtual void ApplyPatches(Harmony harmony)
	{
	}

	protected internal virtual void ApplyLatePatches(Harmony harmony)
	{
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips();
		var definition = DuoArtifactDefinition.GetDefinition(GetType());
		if (definition is null || definition.ExtraTooltips.Count == 0)
			return tooltips;

		tooltips ??= new();
		foreach (var tooltip in definition.ExtraTooltips)
			tooltips.Add(tooltip.MakeTooltip());
		return tooltips;
	}
}
