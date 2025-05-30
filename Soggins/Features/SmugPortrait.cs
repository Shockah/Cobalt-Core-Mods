using CobaltCoreModding.Definitions.ExternalItems;
using HarmonyLib;
using Shockah.Shared;
using System;
using System.Linq;
using System.Reflection;

namespace Shockah.Soggins;

internal static class SmugPortraitManager
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
	}

	private static ExternalAnimation GetClosestAnimation(int smug)
	{
		var smugIndex = smug;
		while (true)
		{
			if (Instance.SmugPortraitAnimations.TryGetValue(smugIndex, out var animation))
				return animation;
			smugIndex -= Math.Sign(smugIndex);
			if ((smug == 0 && smugIndex != 0) || Math.Sign(smug) == -Math.Sign(smugIndex))
				return Instance.NeutralPortraitAnimation;
		}
	}

	private static void Combat_Update_Postfix(Combat __instance, G g)
	{
		QueuedAction.Tick(__instance);

		var character = g.state.characters.FirstOrDefault(c => c.deckType == (Deck)Instance.SogginsDeck.Id!.Value);
		if (character is null)
			return;

		var smug = Instance.Api.GetSmug(g.state, g.state.ship);
		if (smug is null)
		{
			character.loopTag = Instance.NeutralPortraitAnimation.Tag;
			return;
		}

		if (Instance.Api.IsOversmug(g.state, g.state.ship))
		{
			character.loopTag = Instance.OversmugPortraitAnimation.Tag;
			return;
		}

		character.loopTag = GetClosestAnimation(smug.Value).Tag;
	}
}
