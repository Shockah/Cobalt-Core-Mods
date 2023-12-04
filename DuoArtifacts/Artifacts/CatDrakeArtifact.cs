using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatDrakeArtifact : DuoArtifact
{
	private static int SerenityChange = 0;
	private static int TimeStopChange = 0;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_Set_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_Set_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);
	}

	private static void Ship_Set_Prefix(Ship __instance, Status status, ref int __state)
		=> __state = __instance.Get(status);

	private static void Ship_Set_Postfix(Ship __instance, Status status, ref int __state)
	{
		int change = __instance.Get(status) - __state;
		switch (status)
		{
			case Status.serenity:
				SerenityChange += change;
				break;
			case Status.timeStop:
				TimeStopChange += change;
				break;
		}
	}

	private static void Combat_Update_Postfix(Combat __instance, G g)
	{
		if (SerenityChange == 0 && TimeStopChange == 0)
			return;
		int serenityChange = SerenityChange;
		int timeStopChange = TimeStopChange;

		var artifact = g.state.EnumerateAllArtifacts().FirstOrDefault(a => a is CatDrakeArtifact);
		if (artifact is null)
		{
			SerenityChange = 0;
			TimeStopChange = 0;
			return;
		}

		artifact.Pulse();

		if (serenityChange > 0)
			g.state.ship.Add(Status.timeStop, serenityChange);
		if (timeStopChange > 0)
			g.state.ship.Add(Status.serenity, timeStopChange);
		SerenityChange = 0;
		TimeStopChange = 0;
	}
}