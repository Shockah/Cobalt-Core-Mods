using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CustomTTGlossary : TTGlossary
{
	public enum GlossaryType
	{
		midrow, status, cardtrait, action, parttrait, destination, actionMisc, part, env
	}

	private static ModEntry Instance => ModEntry.Instance;

	private static CustomTTGlossary? CurrentContext;

	private readonly GlossaryType Type;
	private readonly Spr? Icon;
	private readonly string Title;
	private readonly string Description;
	private readonly IReadOnlyList<Func<object>> Values;

	public CustomTTGlossary(GlossaryType type, string title, string description, IEnumerable<object> values) : this(type, null, title, description, values) { }

	public CustomTTGlossary(GlossaryType type, string title, string description, IEnumerable<Func<object>>? values = null) : this(type, null, title, description, values) { }

	public CustomTTGlossary(GlossaryType type, Spr? icon, string title, string description, IEnumerable<object> values) : this(type, icon, title, description, values.Select<object, Func<object>>(v => () => v).ToArray()) { }

	public CustomTTGlossary(GlossaryType type, Spr? icon, string title, string description, IEnumerable<Func<object>>? values = null) : base($"{Enum.GetName(type)}.customttglossary")
	{
		this.Type = type;
		this.Icon = icon;
		this.Title = title;
		this.Description = description;
		this.Values = values?.ToList() ?? (IReadOnlyList<Func<object>>)Array.Empty<Func<object>>();
	}

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(BuildIconAndText)),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_BuildIconAndText_Prefix)),
			finalizer: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_BuildIconAndText_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(TTGlossary), "TryGetIcon"),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_TryGetIcon_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(MakeNameDescPair)),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_MakeNameDescPair_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(BuildString)),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_BuildString_Prefix))
		);
	}

	private static void TTGlossary_BuildIconAndText_Prefix(TTGlossary __instance)
		=> CurrentContext = __instance as CustomTTGlossary;

	private static void TTGlossary_BuildIconAndText_Finalizer()
		=> CurrentContext = null;

	private static bool TTGlossary_TryGetIcon_Prefix(ref Spr? __result)
	{
		if (CurrentContext is null)
			return true;

		__result = CurrentContext.Icon;
		return false;
	}

	private static bool TTGlossary_MakeNameDescPair_Prefix(string nameColor, ref string __result)
	{
		if (CurrentContext is null)
			return true;

		__result = $"<c={nameColor}>{CurrentContext.Title.ToUpper()}</c>\n{BuildString(CurrentContext.Description, CurrentContext.Values.Select(v => v()).ToArray())}";
		return false;
	}

	private static bool TTGlossary_BuildString_Prefix(ref string __result)
	{
		if (CurrentContext is null)
			return true;

		object[] args = CurrentContext.Values.Select((object v) => "<c=boldPink>{0}</c>".FF(v.ToString() ?? "")).ToArray();
		__result = string.Format(CurrentContext.Description, args);
		return false;
	}
}
