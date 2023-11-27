using CobaltCoreModding.Definitions.ExternalItems;
using HarmonyLib;
using Shockah.Shared;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal static class EnumPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Enum).GetMethods().First(m => m.Name == "IsDefined" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1).MakeGenericMethod(new Type[] { typeof(ExternalGlossary.GlossayType) }),
			postfix: new HarmonyMethod(typeof(EnumPatches), nameof(Enum_IsDefined_GlossayType_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Enum).GetMethods().First(m => m.Name == "GetName" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1).MakeGenericMethod(new Type[] { typeof(ExternalGlossary.GlossayType) }),
			postfix: new HarmonyMethod(typeof(EnumPatches), nameof(Enum_GetName_GlossayType_Postfix))
		);
	}

	private static void Enum_IsDefined_GlossayType_Postfix(ExternalGlossary.GlossayType value, ref bool __result)
	{
		if (value == ModEntry.StatusGlossaryType)
			__result = true;
	}

	private static void Enum_GetName_GlossayType_Postfix(ExternalGlossary.GlossayType value, ref string? __result)
	{
		if (value == ModEntry.StatusGlossaryType)
			__result = "status";
	}
}