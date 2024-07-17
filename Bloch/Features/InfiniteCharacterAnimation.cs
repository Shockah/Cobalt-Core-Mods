using HarmonyLib;

namespace Shockah.Bloch;

internal sealed class InfiniteCharacterAnimationManager
{
	public InfiniteCharacterAnimationManager()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Shout), nameof(Shout.AnimationFrame)),
			postfix: new HarmonyMethod(GetType(), nameof(Shout_AnimationFrame_Postfix))
		);
	}

	private static void Shout_AnimationFrame_Postfix(Shout __instance, ref double __result)
	{
		if (__instance.who != ModEntry.Instance.BlochDeck.UniqueName)
			return;

		if (__instance.loopTag is "glorp" or "gloop" or "glerp")
			__result = __instance.progress * 12 / 50;
	}
}