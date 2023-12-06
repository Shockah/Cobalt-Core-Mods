using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Wormholes;

internal static class EventsPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Events), nameof(Events.BootSequence)),
			postfix: new HarmonyMethod(typeof(EventsPatches), nameof(Events_BootSequence_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Events), nameof(Events.BootSequenceDownside)),
			postfix: new HarmonyMethod(typeof(EventsPatches), nameof(Events_BootSequenceDownside_Postfix))
		);
	}

	private static void ClearChoiceKeysIfNeeded(List<Choice> choices)
	{
		if (!Instance.UsingWormhole)
			return;

		foreach (var choice in choices)
			choice.key = null;
		Instance.UsingWormhole = false;
	}

	private static void Events_BootSequence_Postfix(ref List<Choice> __result)
		=> ClearChoiceKeysIfNeeded(__result);

	private static void Events_BootSequenceDownside_Postfix(ref List<Choice> __result)
		=> ClearChoiceKeysIfNeeded(__result);
}
