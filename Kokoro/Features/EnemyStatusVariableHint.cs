using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer)
		{
			var copy = Mutil.DeepCopy(action);
			Instance.Api.SetExtensionData(copy, "targetPlayer", targetPlayer);
			return copy;
		}
	}
}

internal sealed class EnemyStatusVariableHintManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AVariableHint), nameof(AVariableHint.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AVariableHint_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__VarAssignment") && m.ReturnType == typeof(void)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_VarAssignment_Transpiler))
		);
	}
	
	private static void AVariableHint_GetTooltips_Postfix(AVariableHint __instance, State s, ref List<Tooltip> __result)
	{
		if (__instance.hand)
			return;
		if (__instance.status is not { } status)
			return;

		var index = __result.FindIndex(t => t is TTGlossary { key: "action.xHint.desc" });
		if (index < 0)
			return;
		if (ModEntry.Instance.Api.ObtainExtensionData(__instance, "targetPlayer", () => true))
			return;

		__result[index] = new GlossaryTooltip($"{typeof(ModEntry).Namespace}::EnemyVariableHint")
		{
			Description = I18n.EnemyVariableHint,
			vals = [
				"<c=status>" + status.GetLocName().ToUpperInvariant() + "</c>",
				(s.route is Combat combat1) ? $" </c>(<c=keyword>{combat1.otherShip.Get(status)}</c>)" : "",
				__instance.secondStatus is { } secondStatus1 ? (" </c>+ <c=status>" + secondStatus1.GetLocName().ToUpperInvariant() + "</c>") : "",
				__instance.secondStatus is { } secondStatus2 && s.route is Combat combat2 ? $" </c>(<c=keyword>{combat2.otherShip.Get(secondStatus2)}</c>)" : ""
			]
		};
	}
	
	private static IEnumerable<CodeInstruction> Card_RenderAction_VarAssignment_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Instruction(OpCodes.Ret))
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "g")),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "action")),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "dontDraw")),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "w")),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_VarAssignment_Transpiler_Outgoing))),
					new CodeInstruction(OpCodes.Stfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "w"))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static int Card_RenderAction_VarAssignment_Transpiler_Outgoing(G g, CardAction action, bool dontDraw, int w)
	{
		if (action is not AVariableHint variableHint)
			return w;
		if (variableHint.hand)
			return w;
		if (ModEntry.Instance.Api.ObtainExtensionData(variableHint, "targetPlayer", () => true))
			return w;

		if (!dontDraw)
		{
			var v = g.Push(null, new Rect(w)).rect.xy;
			var spriteColor = variableHint.disabled ? Colors.disabledIconTint : new Color("ffffff");
			Draw.Sprite(StableSpr.icons_outgoing, v.x, v.y, color: spriteColor);
			g.Pop();
		}
		w += 8;
		return w;
	}
}