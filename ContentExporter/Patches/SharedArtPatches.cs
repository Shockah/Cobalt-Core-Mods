using System.Reflection;
using HarmonyLib;
using Shockah.Shared;

namespace Shockah.ContentExporter;

internal sealed class SharedArtPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static bool DisableDrawing;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(SharedArt), nameof(SharedArt.DrawCore)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SharedArt_DrawCore_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(SharedArt), nameof(SharedArt.ButtonText)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SharedArt_ButtonText_Prefix))
		);
	}

	private static bool SharedArt_DrawCore_Prefix()
		=> !DisableDrawing;

	private static bool SharedArt_ButtonText_Prefix()
		=> !DisableDrawing;
}