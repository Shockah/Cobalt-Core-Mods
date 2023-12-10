using System.Collections.Generic;

namespace Shockah.Soggins;

[ArtifactMeta(unremovable = true)]
internal sealed class SmugArtifact : Artifact
{
	private static ModEntry Instance => ModEntry.Instance;

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTGlossary($"status.{Instance.SmugStatus.Id!.Value}"));
		return tooltips;
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Instance.Api.SetSmug(state.ship, 0);
	}
}
