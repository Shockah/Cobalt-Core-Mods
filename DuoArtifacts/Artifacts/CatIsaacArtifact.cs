using HarmonyLib;
using Nickel;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatIsaacArtifact : DuoArtifact
{
	private const int ChargesPerTurn = 2;

	public int Charges = ChargesPerTurn;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), "BeginCardAction"),
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
		if (a is not ASpawn { fromPlayer: true } action)
			return true;

		var siloPartX = action.fromX ?? g.state.ship.parts.FindIndex(p => p is { active: true, type: PType.missiles });
		if (siloPartX == -1)
			return true;

		var artifact = g.state.EnumerateAllArtifacts().OfType<CatIsaacArtifact>().FirstOrDefault();
		if (artifact is null || artifact.Charges <= 0)
			return true;

		var launchX = g.state.ship.x + siloPartX + action.offset;
		if (CanLaunch(launchX))
			return true;

		var shoveValue = GetShoveValue();
		if (shoveValue == 0)
			return true;

		artifact.Pulse();
		artifact.Charges--;
		__instance.QueueImmediate(action);
		for (var i = 0; i < Math.Abs(shoveValue); i++)
			__instance.QueueImmediate(new AKickMiette
			{
				x = launchX + i * Math.Sign(shoveValue),
				dir = Math.Sign(shoveValue)
			});
		return false;

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

		int GetShoveValue()
		{
			var leftWall = __instance.leftWall ?? int.MinValue;
			var rightWall = __instance.rightWall ?? int.MaxValue;

			for (var i = 1; i < int.MaxValue; i++)
			{
				var leftInWall = launchX - i < leftWall || launchX - i >= rightWall;
				var rightInWall = launchX + i < leftWall || launchX + i >= rightWall;
				if (leftInWall && rightInWall)
					return 0;

				var left = !leftInWall && CanLaunch(launchX - i);
				var right = !rightInWall && CanLaunch(launchX + i);

				if (left && right)
					return g.state.rngActions.NextInt() % 2 == 0 ? -i : i;
				if (left)
					return -i;
				if (right)
					return i;
			}
			return 0;
		}
	}
}