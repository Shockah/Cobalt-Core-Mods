using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatIsaacArtifact : DuoArtifact
{
	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), "BeginCardAction"),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_BeginCardAction_Prefix))
		);
	}

	private static bool Combat_BeginCardAction_Prefix(Combat __instance, G g, CardAction a)
	{
		if (a is not ASpawn action)
			return true;

		int siloPartX = g.state.ship.parts.FindIndex(p => p.active && p.type == PType.missiles);
		if (siloPartX == -1)
			return true;

		var artifact = g.state.EnumerateAllArtifacts().FirstOrDefault(a => a is CatIsaacArtifact);
		if (artifact is null)
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

		artifact.Pulse();
		for (int i = 1; i < int.MaxValue; i++)
		{
			if (CanLaunch(launchX - i))
			{
				__instance.QueueImmediate(action);
				__instance.QueueImmediate(new ADroneMove { dir = i });
				__instance.currentCardAction = null;
				break;
			}
			else if (CanLaunch(launchX + i))
			{
				__instance.QueueImmediate(action);
				__instance.QueueImmediate(new ADroneMove { dir = -i });
				__instance.currentCardAction = null;
				break;
			}
		}
		return false;
	}
}