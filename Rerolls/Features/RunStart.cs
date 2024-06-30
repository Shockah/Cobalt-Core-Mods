using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.Rerolls;

internal sealed class RunStartManager
{
	public RunStartManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => typeof(State).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PopulateRun>") && m.ReturnType == typeof(Route)),
			postfix: new HarmonyMethod(GetType(), nameof(State_PopulateRun_Delegate_Postfix))
		);
	}

	private static void State_PopulateRun_Delegate_Postfix(object __instance)
	{
		if (!DB.artifactMetas.ContainsKey(new RerollArtifact().Key()))
			return; // game not yet ready - probably non-debug warmup

		var delegateType = __instance.GetType();
		var stateField = delegateType.GetFields(AccessTools.all).First(f => f.FieldType == typeof(State));
		var state = (State)stateField.GetValue(__instance)!;
		state.SendArtifactToChar(new RerollArtifact());
	}
}
