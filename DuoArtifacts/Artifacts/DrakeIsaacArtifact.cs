using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeIsaacArtifact : DuoArtifact
{
	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			postfix: new HarmonyMethod(GetType(), nameof(ASpawn_Begin_Postfix))
		);
	}

	private static void ASpawn_Begin_Postfix(ASpawn __instance, State s, Combat c)
	{
		if (!__instance.fromPlayer)
			return;
		if (!c.stuff.TryGetValue(__instance.thing.x, out var @object) || @object != __instance.thing)
			return;
		if (s.ship.Get(Status.heat) <= s.ship.heatMin)
			return;

		var artifact = StateExt.Instance?.EnumerateAllArtifacts().OfType<DrakeIsaacArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		c.QueueImmediate(new AStatus
		{
			status = Status.heat,
			statusAmount = -1,
			targetPlayer = true
		});
		Instance.KokoroApi.AddScorchingStatus(c, __instance.thing, 3);
		artifact.Pulse();
	}
}
