using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal static class ArtifactPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.GetTooltips)),
			postfix: new HarmonyMethod(typeof(ArtifactPatches), nameof(Artifact_GetTooltips_Postfix))
		);
	}

	private static void Artifact_GetTooltips_Postfix(Artifact __instance, ref List<Tooltip> __result)
	{
		var owners = Instance.Database.GetDuoArtifactOwnership(__instance);
		if (owners is null)
			return;

		var duoTextTooltip = I18n.GetDuoArtifactTooltip(owners);
		if (__result.FirstOrDefault() is TTText text)
		{
			var lines = text.text.Split("\n").ToList();
			lines.Insert(1, duoTextTooltip);
			text.text = string.Join("\n", lines);
		}
		else if (__result.Count >= 1)
		{
			__result.Insert(1, new TTText(duoTextTooltip));
		}
		else
		{
			__result.Add(new TTText(duoTextTooltip));
		}
	}
}