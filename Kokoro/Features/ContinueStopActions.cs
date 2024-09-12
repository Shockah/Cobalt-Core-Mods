using HarmonyLib;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public CardAction MakeContinue(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Continue = true };
		}

		public CardAction MakeContinued(Guid id, CardAction action)
			=> new AContinued { Id = id, Continue = true, Action = action };

		public IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeContinued(id, a));

		public CardAction MakeStop(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Continue = false };
		}

		public CardAction MakeStopped(Guid id, CardAction action)
			=> new AContinued { Id = id, Continue = false, Action = action };

		public IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeStopped(id, a));
	}
}

internal sealed class ContinueStopActionManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Postfix))
		);
	}
	
	private static void Combat_DrainCardActions_Prefix(Combat __instance, out bool __state)
		=> __state = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;

	private static void Combat_DrainCardActions_Postfix(Combat __instance, ref bool __state)
	{
		var isWorkingOnActions = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;
		if (isWorkingOnActions || !__state)
			return;

		ModEntry.Instance.Api.ObtainExtensionData(__instance, "ContinueFlags", () => new HashSet<Guid>()).Clear();
		ModEntry.Instance.Api.ObtainExtensionData(__instance, "StopFlags", () => new HashSet<Guid>()).Clear();
	}
}