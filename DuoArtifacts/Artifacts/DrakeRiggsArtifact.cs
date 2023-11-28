using HarmonyLib;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeRiggsArtifact : DuoArtifact, IEvadeHook
{
	private static ModEntry Instance => ModEntry.Instance;

	private bool UsedThisTurn = false;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		Instance.KokoroApi.RegisterEvadeHook(this, -10);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		UsedThisTurn = false;
	}

	bool? IEvadeHook.IsEvadePossible(State state, Combat combat, EvadeHookContext context)
	{
		var artifact = state.artifacts.OfType<DrakeRiggsArtifact>().FirstOrDefault();
		if (artifact is null)
			return null;
		if (artifact.UsedThisTurn)
			return null;
		return true;
	}

	void IEvadeHook.PayForEvade(State state, Combat combat, int direction)
	{
		var artifact = state.artifacts.OfType<DrakeRiggsArtifact>().First();
		artifact.Pulse();
		artifact.UsedThisTurn = true;
		state.ship.Add(Status.heat, 1);
	}
}