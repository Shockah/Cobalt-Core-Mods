using HarmonyLib;
using Microsoft.Xna.Framework;
using Shockah.Shared;

namespace Shockah.Kokoro;

internal static class MGPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(MG), "Draw"),
			postfix: new HarmonyMethod(typeof(MGPatches), nameof(MG_Draw_Prefix))
		);
	}

	private static void MG_Draw_Prefix(GameTime gameTime)
		=> Instance.TotalGameTime = gameTime.TotalGameTime;
}
