using CobaltCoreModding.Definitions.ExternalItems;
using HarmonyLib;
using Shockah.Shared;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class DeckRegistryPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(AccessTools.TypeByName("CobaltCoreModding.Components.Services.DeckRegistry, CobaltCoreModding.Components"), "CobaltCoreModding.Definitions.ModContactPoints.IDeckRegistry.RegisterDeck"),
			postfix: new HarmonyMethod(typeof(DeckRegistryPatches), nameof(DeckRegistry_RegisterCard_Postfix))
		);
	}

	private static void DeckRegistry_RegisterCard_Postfix(ExternalDeck deck, bool __result)
	{
		if (!__result)
			return;
		Instance.CardRenderer.RecordExternalDeck(deck);
	}
}