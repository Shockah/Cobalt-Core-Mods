using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class MaxRiggsArtifact : DuoArtifact, IEvadeHook, IArtifactIconHook
{
	private static ModEntry Instance => ModEntry.Instance;

	public int LastDirection = 0;
	public int Count = 0;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		Instance.KokoroApi.RegisterEvadeHook(this, 0);
		Instance.KokoroApi.RegisterArtifactIconHook(this, 0);
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		if (LastDirection != 0)
			tooltips.Insert(0, new TTText(LastDirection > 0 ? I18n.MaxRiggsArtifactTooltipRight : I18n.MaxRiggsArtifactTooltipLeft));
		return tooltips;
	}

	public override int? GetDisplayNumber(State s)
		=> LastDirection != 0 ? Count : null;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		LastDirection = 0;
		Count = 0;
	}

	void IEvadeHook.AfterEvade(State state, Combat combat, int direction)
	{
		var artifact = state.EnumerateAllArtifacts().OfType<MaxRiggsArtifact>().First();
		if (artifact is null)
			return;

		if (direction != artifact.LastDirection)
		{
			artifact.LastDirection = direction;
			artifact.Count = 0;
		}

		artifact.Count++;
		if (artifact.Count >= 3)
		{
			state.ship.Add(direction > 0 ? Status.autododgeRight : Status.autododgeLeft);
			artifact.LastDirection = 0;
			artifact.Count = 0;
			artifact.Pulse();
		}
	}

	void IArtifactIconHook.OnRenderArtifactIcon(G g, Artifact artifact, Vec position)
	{
		if (artifact is not MaxRiggsArtifact duoArtifact)
			return;
		if (duoArtifact.LastDirection == 0)
			return;

		var sprite = Enum.Parse<Spr>(duoArtifact.LastDirection > 0 ? "icons_autododgeRight" : "icons_autododgeLeft");
		Draw.Sprite(sprite, position.x - 1, position.y - 1);
	}
}