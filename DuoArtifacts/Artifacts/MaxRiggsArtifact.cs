using HarmonyLib;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class MaxRiggsArtifact : DuoArtifact, IEvadeHook, IArtifactIconHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private int LastDirection = 0;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		Instance.KokoroApi.RegisterEvadeHook(this, 0);
		Instance.KokoroApi.RegisterArtifactIconHook(this, 0);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		LastDirection = 0;
	}

	void IEvadeHook.AfterEvade(State state, Combat combat, int direction)
	{
		var artifact = state.EnumerateAllArtifacts().OfType<MaxRiggsArtifact>().First();
		if (artifact is null)
			return;

		if (direction == artifact.LastDirection)
		{
			state.ship.Add(direction > 0 ? Status.autododgeRight : Status.autododgeLeft);
			artifact.LastDirection = 0;
			artifact.Pulse();
		}
		else
		{
			artifact.LastDirection = direction;
		}
	}

	void IArtifactIconHook.OnRenderArtifactIcon(G g, Artifact artifact, Vec position)
	{
		if (artifact is not MaxRiggsArtifact duoArtifact)
			return;
		if (duoArtifact.LastDirection == 0)
			return;

		var sprite = Enum.Parse<Spr>(duoArtifact.LastDirection > 0 ? "icons_autododgeRight" : "icons_autododgeLeft");
		Draw.Sprite(sprite, position.x + 4, position.y + 4);
	}
}