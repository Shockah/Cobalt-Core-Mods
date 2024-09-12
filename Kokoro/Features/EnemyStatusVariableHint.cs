using HarmonyLib;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer)
		{
			var copy = Mutil.DeepCopy(action);
			Instance.Api.SetExtensionData(copy, "targetPlayer", targetPlayer);
			return copy;
		}
	}
}

internal sealed class EnemyStatusVariableHintManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AVariableHint), nameof(AVariableHint.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AVariableHint_GetTooltips_Postfix))
		);
	}
	
	private static void AVariableHint_GetTooltips_Postfix(AVariableHint __instance, State s, ref List<Tooltip> __result)
	{
		if (__instance.hand)
			return;
		if (__instance.status is not { } status)
			return;

		var index = __result.FindIndex(t => t is TTGlossary { key: "action.xHint.desc" });
		if (index < 0)
			return;
		if (ModEntry.Instance.Api.ObtainExtensionData(__instance, "targetPlayer", () => true))
			return;

		__result[index] = new CustomTTGlossary(
			CustomTTGlossary.GlossaryType.action,
			() => null,
			() => "",
			() => I18n.EnemyVariableHint,
			[
				() => "<c=status>" + status.GetLocName().ToUpperInvariant() + "</c>",
				() => (s.route is Combat combat1) ? $" </c>(<c=keyword>{combat1.otherShip.Get(status)}</c>)" : "",
				() => __instance.secondStatus is { } secondStatus1 ? (" </c>+ <c=status>" + secondStatus1.GetLocName().ToUpperInvariant() + "</c>") : "",
				() => __instance.secondStatus is { } secondStatus2 && s.route is Combat combat2 ? $" </c>(<c=keyword>{combat2.otherShip.Get(secondStatus2)}</c>)" : ""
			]
		);
	}
}