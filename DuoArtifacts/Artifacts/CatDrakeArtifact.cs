using HarmonyLib;
using Nickel;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatDrakeArtifact : DuoArtifact
{
	private static int SerenityChange;
	private static int TimeStopChange;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_Set_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_Set_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);
	}

	private static void Ship_Set_Prefix(Ship __instance, Status status, out int __state)
		=> __state = __instance.Get(status);

	private static void Ship_Set_Postfix(Ship __instance, Status status, ref int __state)
	{
		var change = __instance.Get(status) - __state;
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

	private static void Combat_Update_Postfix(G g)
	{
		if (SerenityChange == 0 && TimeStopChange == 0)
			return;
		var serenityChange = SerenityChange;
		var timeStopChange = TimeStopChange;

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