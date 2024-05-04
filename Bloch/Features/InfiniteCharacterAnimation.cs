using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class InfiniteCharacterAnimationManager
{
	public InfiniteCharacterAnimationManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Shout), nameof(Shout.AnimationFrame)),
			postfix: new HarmonyMethod(GetType(), nameof(Shout_AnimationFrame_Postfix))
		);
	}

	private static void Shout_AnimationFrame_Postfix(Shout __instance, ref double __result)
	{
		if (__instance.who != ModEntry.Instance.BlochDeck.UniqueName)
			return;

		if (__instance.loopTag == "glorp")
			__result = __instance.progress * 12 / 50;
	}
}