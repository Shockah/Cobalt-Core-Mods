using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class CombatPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_DrainCardActions_Prefix)),
			postfix: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_DrainCardActions_Postfix))
		);
	}

	private static void Combat_DrainCardActions_Prefix(Combat __instance, out bool __state)
		=> __state = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;

	private static void Combat_DrainCardActions_Postfix(Combat __instance, ref bool __state)
	{
		var isWorkingOnActions = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;
		if (isWorkingOnActions || !__state)
			return;

		Instance.Api.ObtainExtensionData(__instance, "ContinueFlags", () => new HashSet<Guid>()).Clear();
		Instance.Api.ObtainExtensionData(__instance, "StopFlags", () => new HashSet<Guid>()).Clear();
	}
}