using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

public abstract class DuoArtifact : Artifact
{
	protected static ModEntry Instance => ModEntry.Instance;

	protected internal virtual void ApplyPatches(Harmony harmony)
	{
	}

	protected internal virtual void ApplyLatePatches(Harmony harmony)
	{
	}

	protected internal virtual void RegisterArt(ISpriteRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
	}

	protected internal virtual void RegisterStatuses(IStatusRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
	}

	protected internal virtual void RegisterCards(ICardRegistry registry, string namePrefix, DuoArtifactDefinition definition)
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
