using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Rerolls;

internal static class EventsPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Events), nameof(Events.NewShop)),
			postfix: new HarmonyMethod(typeof(EventsPatches), nameof(Events_NewShop_Postfix))
		);
	}

	private static void Events_NewShop_Postfix(State s, ref List<Choice> __result)
	{
		var artifact = s.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		int indexToAppend = __result.FindIndex(c => c.key == ".shopAboutToDestroyYou");
		if (indexToAppend == -1)
			indexToAppend = __result.FindIndex(c => c.key == ".shopSkip_Confirm");

		__result.Insert(indexToAppend, new Choice
		{
			label = I18n.ShopOption,
			key = ".shopUpgradeCard",
			actions =
			{
				new ADelegateAction((_, state, _) =>
				{
					artifact.RerollsLeft++;
					artifact.Pulse();
				})
			}
		});
	}
}
