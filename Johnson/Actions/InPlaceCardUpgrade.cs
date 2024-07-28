﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nickel;
using System.Reflection;

namespace Shockah.Johnson;

public sealed class InPlaceCardUpgrade : CardUpgrade
{
	internal static void ApplyPatches(IHarmony harmony, ILogger logger)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardUpgrade), nameof(FinallyReallyUpgrade)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(FinallyReallyUpgrade_Prefix))
		);
	}

	private static bool FinallyReallyUpgrade_Prefix(CardUpgrade __instance, G g, Card newCard)
	{
		if (__instance is not InPlaceCardUpgrade)
			return true;

		var card = g.state.FindCard(newCard.uuid);
		if (card is null)
			return true;

		card.upgrade = newCard.upgrade;
		return false;
	}
}
