using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Rerolls;

internal sealed class RerollChargeManager
{
	public RerollChargeManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Events), nameof(Events.NewShop)),
			postfix: new HarmonyMethod(GetType(), nameof(Events_NewShop_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(MapExit), nameof(MapExit.MakeRoute)),
			postfix: new HarmonyMethod(GetType(), nameof(MapExit_MakeRoute_Postfix))
		);
	}

	private static void GrantReroll(State state)
	{
		if (state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault() is not { } artifact)
			return;

		artifact.RerollsLeft++;
		artifact.Pulse();
	}

	private static void Events_NewShop_Postfix(State s, ref List<Choice> __result)
	{
		if (s.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault() is not { } artifact)
			return;

		var indexToAppend = __result.FindIndex(c => c.key == ".shopAboutToDestroyYou");
		if (indexToAppend == -1)
			indexToAppend = __result.FindIndex(c => c.key == ".shopSkip_Confirm");

		__result.Insert(indexToAppend, new Choice
		{
			label = ModEntry.Instance.Localizations.Localize(["shopChoice"]),
			key = ".shopUpgradeCard",
			actions = [new GrantRerollAction()]
		});
	}

	private static void MapExit_MakeRoute_Postfix(State s)
		=> GrantReroll(s);

	private sealed class GrantRerollAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			GrantReroll(s);
		}
	}
}
