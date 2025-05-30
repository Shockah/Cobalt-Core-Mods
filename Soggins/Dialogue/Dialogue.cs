using System;
using System.Reflection;
using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Soggins;

internal static class Dialogue
{
	internal const string CurrentSmugLoopTag = "CurrentSmugLoopTag";

	private static ModEntry Instance => ModEntry.Instance;

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(MG), "DrawLoadingScreen"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MG_DrawLoadingScreen_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MG_DrawLoadingScreen_Postfix))
		);
	}

	internal static void Inject()
	{
		CustomSay.RegisteredDynamicLoopTags[CurrentSmugLoopTag] = CurrentSmugLoopTagFunction;

		EventDialogue.Inject();
		SmugDialogue.Inject();
		ArtifactDialogue.Inject();
		CombatDialogue.Inject();

		foreach (var cardType in ModEntry.AllCards)
		{
			if (Activator.CreateInstance(cardType) is not IRegisterableCard card)
				continue;
			card.InjectDialogue();
		}
	}

	private static string CurrentSmugLoopTagFunction(G g)
	{
		if (Instance.Api.IsOversmug(g.state, g.state.ship))
			return Instance.OversmugPortraitAnimation.Tag;
		var smug = Instance.Api.GetSmug(g.state, g.state.ship) ?? 0;
		return Instance.SmugPortraitAnimations[smug].Tag;
	}

	private static void MG_DrawLoadingScreen_Prefix(MG __instance, out int __state)
		=> __state = __instance.loadingQueue?.Count ?? 0;

	private static void MG_DrawLoadingScreen_Postfix(MG __instance, ref int __state)
	{
		if (__state <= 0)
			return;
		if ((__instance.loadingQueue?.Count ?? 0) > 0)
			return;
		Dialogue.Inject();
	}
}