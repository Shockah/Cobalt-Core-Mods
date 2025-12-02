using HarmonyLib;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.ContentExporter;

internal static class EditorPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Editor), "PanelTools"),
			transpiler: new HarmonyMethod(typeof(EditorPatches), nameof(Editor_PanelTools_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> Editor_PanelTools_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldstr("(en)"),
					ILMatches.Call("Button"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				)
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EditorPatches), nameof(Editor_PanelTools_Transpiler_AddButtons))).WithLabels(labels)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Editor_PanelTools_Transpiler_AddButtons()
	{
		ImGui.SameLine();
		if (ImGui.Button("CAT card export"))
			Instance.QueueTask(g => Instance.QueueSelectedDecksExportTask(g, withScreenFilter: false));

		ImGui.SameLine();
		if (ImGui.Button("(bluish)"))
			Instance.QueueTask(g => Instance.QueueSelectedDecksExportTask(g, withScreenFilter: true));

		if (ImGui.Button("CAT artifact export"))
			Instance.QueueTask(g => Instance.QueueAllArtifactsExportTask(g, withScreenFilter: false));

		ImGui.SameLine();
		if (ImGui.Button("(bluish2)"))
			Instance.QueueTask(g => Instance.QueueAllArtifactsExportTask(g, withScreenFilter: true));

		if (ImGui.Button("CAT ship export"))
			Instance.QueueTask(_ => Instance.QueueAllShipsExportTask(withScreenFilter: false));

		ImGui.SameLine();
		if (ImGui.Button("(bluish3)"))
			Instance.QueueTask(_ => Instance.QueueAllShipsExportTask(withScreenFilter: true));
	}
}