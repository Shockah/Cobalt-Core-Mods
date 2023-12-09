using HarmonyLib;
using Shockah.Shared;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatIsaacArtifact : DuoArtifact
{
	private const int ChargesPerTurn = 2;

	public int Charges = ChargesPerTurn;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), "BeginCardAction"),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_BeginCardAction_Prefix))
		);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		Charges = ChargesPerTurn;
	}

	public override int? GetDisplayNumber(State s)
		=> Charges;

	private static bool Combat_BeginCardAction_Prefix(Combat __instance, G g, CardAction a)
	{
		if (a is not ASpawn action || !action.fromPlayer)
			return true;

		int siloPartX = g.state.ship.parts.FindIndex(p => p.active && p.type == PType.missiles);
		if (siloPartX == -1)
			return true;

		var artifact = g.state.EnumerateAllArtifacts().OfType<CatIsaacArtifact>().FirstOrDefault();
		if (artifact is null || artifact.Charges <= 0)
			return true;

		bool CanLaunch(int x)
		{
			if (!__instance.stuff.TryGetValue(x, out var @object))
				return true;
			if (@object.Invincible() || @object.IsFriendly())
				return false;
			if (@object is SpaceMine)
				return false;
			if (@object.fromPlayer)
				return false;
			return true;
		}

		int launchX = g.state.ship.x + siloPartX + action.offset;
		if (CanLaunch(launchX))
			return true;

		int GetShoveValue()
		{
			int leftWall = __instance.leftWall ?? int.MinValue;
			int rightWall = __instance.rightWall ?? int.MaxValue;

			for (int i = 1; i < int.MaxValue; i++)
			{
				bool leftInWall = launchX - i < leftWall || launchX - i >= rightWall;
				bool rightInWall = launchX + i < leftWall || launchX + i >= rightWall;
				if (leftInWall && rightInWall)
					return 0;

				bool left = !leftInWall && CanLaunch(launchX - i);
				bool right = !rightInWall && CanLaunch(launchX + i);

				if (left && right)
					return g.state.rngActions.NextInt() % 2 == 0 ? -i : i;
				else if (left)
					return -i;
				else if (right)
					return i;
			}
			return 0;
		}

		int shoveValue = GetShoveValue();
		if (shoveValue == 0)
			return true;

		artifact.Pulse();
		artifact.Charges--;
		__instance.QueueImmediate(action);
		for (int i = 0; i < Math.Abs(shoveValue); i++)
		{
			__instance.QueueImmediate(new AKickMiette
			{
				x = launchX + i * Math.Sign(shoveValue),
				dir = Math.Sign(shoveValue)
			});
		}
		return false;
	}
}