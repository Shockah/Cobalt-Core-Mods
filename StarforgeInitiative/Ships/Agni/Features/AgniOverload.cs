using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class AgniOverload : IRegisterable
{
	private static ISpriteEntry CardGlowSprite = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		CardGlowSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Agni/CardGlow.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var glowColor = il.DeclareLocal(typeof(Color?));
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Call(nameof(Card.GetDataWithOverrides)),
					ILMatches.Stloc<CardData>(originalMethod).GetLocalIndex(out var dataLocalIndex),
				])
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldloc<State>(originalMethod).GetLocalIndex(out var stateLocalIndex),
					ILMatches.Call(nameof(Card.GetCurrentCost)),
					ILMatches.Stloc<int>(originalMethod).GetLocalIndex(out var costLocalIndex),
				])
				.Find([
					ILMatches.Call(nameof(G.Push)),
					ILMatches.Stloc<Box>(originalMethod).GetLocalIndex(out var boxLocalIndex),
				])
				.Find(ILMatches.Ldarg(8))
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, dataLocalIndex.Value).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, stateLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, costLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_RenderBehindGlow))),
					new CodeInstruction(OpCodes.Stloc, glowColor),
				])
				.Find([
					ILMatches.LdcI4(StableSpr.cardShared_card_outline),
					ILMatches.Newobj(AccessTools.DeclaredConstructor(typeof(Spr?), [typeof(Spr)])),
				])
				.Find(ILMatches.Call(nameof(Draw.Sprite)))
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels2)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value).WithLabels(labels2),
					new CodeInstruction(OpCodes.Ldloc, glowColor),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_RenderOutlineGlow))),
				])
				.Find([
					ILMatches.Ldarg(1).ExtractLabels(out var labels3),
					ILMatches.Call(nameof(G.Pop)),
					ILMatches.Instruction(OpCodes.Ret),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value).WithLabels(labels3),
					new CodeInstruction(OpCodes.Ldloc, glowColor),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_RenderOnTopGlow))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}
	
	private static Color? Card_Render_Transpiler_RenderBehindGlow(CardData data, G g, Box box, State state, int cost)
	{
		if (state != g.state)
			return null;
		if (g.state.route is not Combat combat)
			return null;
		if (combat.energy >= cost)
			return null;
		if (data.unplayable)
			return null;
		if (!state.EnumerateAllArtifacts().Any(a => a is AgniGeneratorOverloadArtifact))
			return null;
		
		var missingEnergy = cost - combat.energy;
		if (missingEnergy > 1 && !state.EnumerateAllArtifacts().Any(a => a is AgniUpgradedCapacitorsArtifact))
			return null;
		
		var v = box.rect.xy + new Vec(0, 1);
		var glowColor = new Color("FF7828");
		glowColor = Color.Lerp(glowColor, new Color("B42814"), Mutil.Remap(-1, 1, 0, 1, Math.Sin((g.time + v.x / g.mg.PIX_W) * 1)));
		glowColor = Color.Lerp(glowColor, new Color("FFC850"), Math.Clamp(Mutil.Remap(-1, 1, -4, 0.15, Math.Sin((g.time + v.x / g.mg.PIX_W) * 3.84)), 0, 1));
		glowColor = Color.Lerp(glowColor, new Color("FFC850"), Math.Clamp(Mutil.Remap(-1, 1, -4, 0.15, Math.Sin((g.time + v.x / g.mg.PIX_W) * 2.465)), 0, 1));
		glowColor = Color.Lerp(glowColor, Colors.black, Mutil.Remap(-1, 1, 0.15, 0.2, Math.Sin((g.time + v.x / g.mg.PIX_W) * 15)));
		
		Draw.Sprite(
			CardGlowSprite.Sprite,
			v.x - 5, v.y - 6,
			color: glowColor,
			blend: BlendMode.Screen
		);
		return glowColor;
	}
	
	private static void Card_Render_Transpiler_RenderOutlineGlow(Box box, Color? glowColor)
	{
		if (glowColor is null)
			return;
		
		var v = box.rect.xy + new Vec(0, 1);
		Draw.Sprite(
			StableSpr.cardShared_card_outline,
			v.x - 1, v.y - 2,
			color: glowColor.Value,
			blend: BlendMode.Screen
		);
	}
	
	private static void Card_Render_Transpiler_RenderOnTopGlow(Box box, Color? glowColor)
	{
		if (glowColor is null)
			return;
		
		var v = box.rect.xy + new Vec(0, 1);
		Draw.Sprite(
			CardGlowSprite.Sprite,
			v.x - 5, v.y - 6,
			color: glowColor.Value.gain(0.2),
			blend: BlendMode.Screen
		);
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var didAllowExpensivePlayAndShouldEndTurnLocal = il.DeclareLocal(typeof(bool));
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod).GetLocalIndex(out var dataLocalIndex),
					ILMatches.Ldfld(nameof(CardData.exhaust)),
					ILMatches.Ldarg(4),
					ILMatches.Instruction(OpCodes.Or),
					ILMatches.Stloc<bool>(originalMethod).GetLocalIndex(out var actuallyExhaustLocalIndex),
				])
				.Find([
					ILMatches.Ldloc<int>(originalMethod).GetLocalIndex(out var costLocalIndex).ExtractLabels(out var labels),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(Combat.energy)),
					ILMatches.Bgt,
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldloca, costLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloca, actuallyExhaustLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_AllowExpensivePlay))),
					new CodeInstruction(OpCodes.Stloc, didAllowExpensivePlayAndShouldEndTurnLocal),
				])
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldloc<List<CardAction>>(originalMethod),
				])
				.Find(ILMatches.Call(nameof(Combat.Queue)))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldloc, dataLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, didAllowExpensivePlayAndShouldEndTurnLocal),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_EndTurnIfNeeded))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static bool Combat_TryPlayCard_Transpiler_AllowExpensivePlay(State state, Combat combat, Card card, ref int cost, ref bool actuallyExhaust)
	{
		if (combat.energy >= cost)
			return false;
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is AgniGeneratorOverloadArtifact) is not { } artifact)
			return false;

		var missingEnergy = cost - combat.energy;
		if (missingEnergy > 1)
		{
			if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is AgniUpgradedCapacitorsArtifact) is { } capacitorsArtifact)
				capacitorsArtifact.Pulse();
			else
				return false;
		}

		cost = combat.energy;
		actuallyExhaust = false;
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ModEntry.Instance.Helper.Content.Cards.UnplayableCardTrait, true, false);
		
		artifact.Pulse();
		return true;
	}

	private static void Combat_TryPlayCard_Transpiler_EndTurnIfNeeded(State state, Combat combat, CardData data, bool didAllowExpensivePlayAndShouldEndTurn)
	{
		if (!didAllowExpensivePlayAndShouldEndTurn)
			return;

		if (data.singleUse)
			combat.Queue(new AAddCard { card = new TrashUnplayable(), destination = CardDestination.Discard });

		if (state.EnumerateAllArtifacts().OfType<AgniWaterCoolingArtifact>().FirstOrDefault() is { TriggeredThisTurn: false } artifact)
		{
			artifact.TriggeredThisTurn = true;
			artifact.Pulse();
		}
		else
		{
			combat.Queue(new AEndTurn());
		}
	}
}