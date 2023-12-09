using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal static class CardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(Card_GetAllTooltips_Postfix))
		);
	}

	private static void Card_GetAllTooltips_Postfix(ref IEnumerable<Tooltip> __result)
	{
		var result = __result;
		IEnumerable<Tooltip> ModifyResult()
		{
			foreach (var tooltip in result)
			{
				if (tooltip is TTGlossary glossary && glossary.key == $"status.{Instance.Content.WormStatus.Id!.Value}" && (glossary.vals is null || glossary.vals.Length == 0 || Equals(glossary.vals[0], "<c=boldPink>0</c>")))
					glossary.vals = new object[] { "<c=boldPink>1</c>" };
				yield return tooltip;
			}
		}
		__result = ModifyResult();
	}
}
