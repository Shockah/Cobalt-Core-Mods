using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using daisyowl.text;
using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using TextCopy;

namespace Shockah.CustomRunOptions;

internal sealed class CopySeedOnRunSummary : IRegisterable
{
	private static readonly UK CopySeedUk = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunSummaryRoute), nameof(RunSummaryRoute.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Transpiler))
		);
	}
	
	private static IEnumerable<CodeInstruction> RunSummaryRoute_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldfld("runSummary"),
					ILMatches.Ldflda("seed"),
				])
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After, ILMatches.Call("Text"))
				.Replace([
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Transpiler_HijackDrawSeedText))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static Rect RunSummaryRoute_Render_Transpiler_HijackDrawSeedText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale, RunSummaryRoute route, G g)
	{
		var baseBox = g.Push();
		
		var rect = Draw.Text(str, x, y, font, color, colorForce, progress, maxWidth, align, true, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);
		if (dontDraw)
			return rect;

		var box = g.Push(CopySeedUk, new Rect(x - baseBox.rect.x + rect.x - 2, y - baseBox.rect.y + rect.y - 2, rect.w + 4, rect.h + 4), onMouseDown: new MouseDownHandler(() =>
		{
			Audio.Play(Event.Click);
			ClipboardService.SetText(route.runSummary.seed.ToString());
		}));

		var isHover = box.IsHover();
		var finalColor = isHover ? Colors.textChoiceHoverActive : color;
		
		Draw.Text(str, x, y, font, finalColor, colorForce, progress, maxWidth, align, false, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);

		g.Pop();
		g.Pop();
		return rect;
	}
}