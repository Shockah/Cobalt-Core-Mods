using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class BooksMaxArtifact : DuoArtifact
{
	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToExhaust)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_SendCardToExhaust_Postfix))
		);
	}

	private static void Combat_SendCardToExhaust_Postfix(Combat __instance, State s)
	{
		var artifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksMaxArtifact);
		if (artifact is null)
			return;

		__instance.QueueImmediate(new AStatus
		{
			status = Status.shard,
			statusAmount = 1,
			targetPlayer = true
		});
		artifact.Pulse();
	}
}