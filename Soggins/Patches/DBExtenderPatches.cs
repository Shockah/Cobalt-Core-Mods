using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Soggins;

internal static class DBExtenderPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void ApplyLatePatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(AccessTools.TypeByName("CobaltCoreModding.Components.Services.DBExtender, CobaltCoreModding.Components"), "PatchMetasAndStoryFunctions"),
			postfix: new HarmonyMethod(typeof(DBExtenderPatches), nameof(DBExtender_InitSync_Postfix))
		);
	}

	private static void AddToSogginsDeck(CardMeta meta)
	{
		meta.deck = (Deck)Instance.SogginsDeck.Id!.Value;
		meta.dontOffer = false;
	}

	private static void DBExtender_InitSync_Postfix()
	{
		AddToSogginsDeck(new MissileMalware().GetMeta());
		AddToSogginsDeck(new SeekerMissileCard().GetMeta());
	}
}
