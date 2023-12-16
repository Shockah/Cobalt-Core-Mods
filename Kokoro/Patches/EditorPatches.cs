using HarmonyLib;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class EditorPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static int CardUpgradeToCreateValue = (int)Upgrade.None;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredConstructor(typeof(Editor), Array.Empty<Type>()),
			postfix: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_ctor_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Editor), "PanelStatuses"),
			transpiler: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelStatuses_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Editor).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PanelStatuses>") && m.ReturnType == typeof(bool)),
			postfix: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelStatuses_WhereDelegate_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Editor), "PanelArtifacts"),
			transpiler: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelArtifacts_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Editor).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PanelArtifacts>") && m.ReturnType == typeof(bool)),
			postfix: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelArtifacts_WhereDelegate_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Editor), "PanelCards"),
			transpiler: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelCards_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Editor).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PanelCards>") && m.ReturnType == typeof(bool)),
			postfix: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelCards_WhereDelegate_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Editor).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PanelCards>g__selectNode") && m.ReturnType == typeof(void)),
			transpiler: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelCards_SelectNodeDelegate_Transpiler))
		);
	}

	private static string? OptionalLoc(string key)
		=> DB.currentLocale.strings.TryGetValue(key, out var localized) ? localized : null;

	private static string OptionalLoc(string formatWithLoc, string key, string @default)
	{
		if (DB.currentLocale.strings.TryGetValue(key, out var localized))
			return string.Format(formatWithLoc, @default, localized);
		return @default;
	}

	private static void Editor_ctor_Postfix(Editor __instance)
		=> __instance.allDecks = __instance.allDecks
			.Concat(DB.decks.Keys)
			.Distinct()
			.ToList();

	private static IEnumerable<CodeInstruction> Editor_PanelStatuses_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloca<Status>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Instruction(OpCodes.Constrained),
					ILMatches.Call("ToString"),
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("cursorPosStatus"),
					ILMatches.Instruction(OpCodes.Ceq),
					ILMatches.Call("Selectable")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocStatus)
				.Advance(7)
				.Replace(
					ldlocStatus,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EditorPatches), nameof(Editor_PanelStatuses_Transpiler_ImGuiSelectableHijack)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Editor_PanelStatuses_Transpiler_ImGuiSelectableHijack(string label, bool isSelected, Status status)
		=> ImGui.Selectable($"{status.GetLocName()} ({label})", isSelected);

	private static void Editor_PanelStatuses_WhereDelegate_Postfix(Status __0, ref bool __result)
		=> __result = GExt.Instance?.e is { } e && (__0.Key().Contains(e.searchStrStatus, StringComparison.OrdinalIgnoreCase) || __0.GetLocName().Contains(e.searchStrStatus, StringComparison.OrdinalIgnoreCase));

	private static IEnumerable<CodeInstruction> Editor_PanelArtifacts_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloca<KeyValuePair<string, Type>>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Call("get_Key"),
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("cursorPosArtifact"),
					ILMatches.Instruction(OpCodes.Ceq),
					ILMatches.Call("Selectable")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocKvp)
				.Advance(6)
				.Replace(
					ldlocKvp,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EditorPatches), nameof(Editor_PanelArtifacts_Transpiler_ImGuiSelectableHijack)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Editor_PanelArtifacts_Transpiler_ImGuiSelectableHijack(string label, bool isSelected, KeyValuePair<string, Type> kvp)
		=> ImGui.Selectable(OptionalLoc("{1} ({0})", $"artifact.{kvp.Key}.name", label), isSelected);

	private static void Editor_PanelArtifacts_WhereDelegate_Postfix(KeyValuePair<string, Type> __0, ref bool __result)
		=> __result = GExt.Instance?.e is { } e && (__0.Key.Contains(e.searchStrArtifact, StringComparison.OrdinalIgnoreCase) || OptionalLoc($"artifact.{__0.Key}.name")?.Contains(e.searchStrArtifact, StringComparison.OrdinalIgnoreCase) == true);

	private static IEnumerable<CodeInstruction> Editor_PanelCards_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldstr("Cards"),
					ILMatches.Call("GetContentRegionAvail"),
					ILMatches.Call("BeginListBox"),
					ILMatches.Brfalse
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EditorPatches), nameof(Editor_PanelCards_Transpiler_AddUpgradeRadios))).WithLabels(labels)
				)
				.Find(
					ILMatches.Ldloca<KeyValuePair<string, Type>>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Call("get_Key"),
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("cursorPosCards"),
					ILMatches.Instruction(OpCodes.Ceq),
					ILMatches.Call("Selectable")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocKvp)
				.Advance(6)
				.Replace(
					ldlocKvp,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EditorPatches), nameof(Editor_PanelCards_Transpiler_ImGuiSelectableHijack)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Editor_PanelCards_Transpiler_AddUpgradeRadios()
	{
		ImGui.RadioButton("Unupgraded", ref CardUpgradeToCreateValue, (int)Upgrade.None);
		ImGui.SameLine();
		ImGui.RadioButton("A", ref CardUpgradeToCreateValue, (int)Upgrade.A);
		ImGui.SameLine();
		ImGui.RadioButton("B", ref CardUpgradeToCreateValue, (int)Upgrade.B);
	}

	private static bool Editor_PanelCards_Transpiler_ImGuiSelectableHijack(string label, bool isSelected, KeyValuePair<string, Type> kvp)
		=> ImGui.Selectable(OptionalLoc("{1} ({0})", $"card.{kvp.Key}.name", label), isSelected);

	private static void Editor_PanelCards_WhereDelegate_Postfix(KeyValuePair<string, Type> __0, ref bool __result)
		=> __result = GExt.Instance?.e is { } e && (__0.Key.Contains(e.searchStrCards, StringComparison.OrdinalIgnoreCase) || OptionalLoc($"card.{__0.Key}.name")?.Contains(e.searchStrCards, StringComparison.OrdinalIgnoreCase) == true);

	private static IEnumerable<CodeInstruction> Editor_PanelCards_SelectNodeDelegate_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stloc<Card>(originalMethod.GetMethodBody()!.LocalVariables))
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EditorPatches), nameof(Editor_PanelCards_SelectNodeDelegate_Transpiler_ModifyCard)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Card Editor_PanelCards_SelectNodeDelegate_Transpiler_ModifyCard(Card card)
	{
		var targetUpgrade = (Upgrade)CardUpgradeToCreateValue;
		if (card.GetMeta().upgradesTo.Contains(targetUpgrade))
			card.upgrade = targetUpgrade;
		return card;
	}
}