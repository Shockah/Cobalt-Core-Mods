using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.Rerolls;

internal static class StatePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(State).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PopulateRun>") && m.ReturnType == typeof(Route)),
			postfix: new HarmonyMethod(typeof(StatePatches), nameof(State_PopulateRun_Delegate_Postfix))
		);
	}

	private static void State_PopulateRun_Delegate_Postfix(object __instance)
	{
		var delegateType = __instance.GetType();
		var stateField = delegateType.GetFields(AccessTools.all).First(f => f.FieldType == typeof(State));
		var state = (State)stateField.GetValue(__instance)!;
		state.SendArtifactToChar(new RerollArtifact());
	}
}
