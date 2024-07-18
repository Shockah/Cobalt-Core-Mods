﻿using HarmonyLib;
using Nickel;
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

	private static readonly Stack<TTGlossary> ContextStack = new();

	private static int NextID = 0;
	private readonly GlossaryType Type;
	private readonly Func<Spr?> Icon;
	private readonly Func<string> Title;
	private readonly Func<string> Description;
	private readonly IReadOnlyList<Func<object>> Values;

	public CustomTTGlossary(GlossaryType type, Func<string> title, Func<string> description, IEnumerable<Func<object>>? values = null) : this(type, () => null, title, description, values) { }

	public CustomTTGlossary(GlossaryType type, Func<Spr?> icon, Func<string> title, Func<string> description, IEnumerable<Func<object>>? values = null) : base($"{Enum.GetName(type)}.customttglossary.{NextID++}")
	{
		this.Type = type;
		this.Icon = icon;
		this.Title = title;
		this.Description = description;
		this.Values = values?.ToList() ?? (IReadOnlyList<Func<object>>)Array.Empty<Func<object>>();
	}

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(BuildIconAndText)),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_BuildIconAndText_Prefix)),
			finalizer: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_BuildIconAndText_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(TTGlossary), "TryGetIcon"),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_TryGetIcon_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(MakeNameDescPair)),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_MakeNameDescPair_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(TTGlossary), nameof(BuildString)),
			prefix: new HarmonyMethod(typeof(CustomTTGlossary), nameof(TTGlossary_BuildString_Prefix))
		);
	}

	private static void TTGlossary_BuildIconAndText_Prefix(TTGlossary __instance)
		=> ContextStack.Push(__instance);

	private static void TTGlossary_BuildIconAndText_Finalizer()
		=> ContextStack.Pop();

	private static bool TTGlossary_TryGetIcon_Prefix(ref Spr? __result)
	{
		if (!ContextStack.TryPeek(out var glossary) || glossary is not CustomTTGlossary custom)
			return true;

		__result = custom.Icon();
		return false;
	}

	private static bool TTGlossary_MakeNameDescPair_Prefix(string nameColor, ref string __result)
	{
		if (!ContextStack.TryPeek(out var glossary) || glossary is not CustomTTGlossary custom)
			return true;

		__result = $"<c={nameColor}>{custom.Title().ToUpper()}</c>\n{BuildString(custom.Description(), custom.Values.Select(v => v()).ToArray())}";
		return false;
	}

	private static bool TTGlossary_BuildString_Prefix(ref string __result)
	{
		if (!ContextStack.TryPeek(out var glossary) || glossary is not CustomTTGlossary custom)
			return true;

		object[] args = custom.Values.Select(v => "<c=boldPink>{0}</c>".FF(v().ToString() ?? "")).ToArray();
		__result = string.Format(custom.Description(), args);
		return false;
	}
}
