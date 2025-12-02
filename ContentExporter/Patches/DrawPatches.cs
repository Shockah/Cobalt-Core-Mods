using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.ContentExporter;

internal sealed class DrawPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static (Spr Original, Spr Replacement)? ReplacementSprite;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger,
			original: () => typeof(Draw).GetMethods().First(m => m.Name == nameof(Draw.Sprite) && m.GetParameters()[0].ParameterType == typeof(Spr?)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Draw_Sprite_Prefix))
		);
	}

	private static void Draw_Sprite_Prefix(ref Spr? id)
	{
		if (ReplacementSprite is not { } replacementSprite)
			return;
		if (replacementSprite.Original != id)
			return;
		id = replacementSprite.Replacement;
	}
}