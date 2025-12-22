using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.Dracula;

internal sealed class MavisManager
{
	public MavisManager()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetIcon)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetIcon_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}

	// TODO: no longer needed in next game update
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var droneLocalIndex).ExtractLabels(out var labels),
					ILMatches.Isinst<AttackDrone>(),
					ILMatches.Stloc<AttackDrone>(originalMethod),
					ILMatches.Ldloc<AttackDrone>(originalMethod),
					ILMatches.Brfalse,
					ILMatches.Ldloc<AttackDrone>(originalMethod),
					ILMatches.LdcR8(1),
					ILMatches.Stfld(nameof(StuffBase.pulse)),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, droneLocalIndex.Value).WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler_PulseMavis))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void AAttack_Begin_Transpiler_PulseMavis(StuffBase @object)
	{
		if (@object is MavisStuff)
			@object.pulse = 1;
	}

	private static void AAttack_GetIcon_Postfix(AAttack __instance, ref Icon? __result)
	{
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(__instance, "MavisSwoopDir") is not { } swoopDir)
			return;

		__result = new()
		{
			path = swoopDir switch
			{
				< 0 => ModEntry.Instance.SweepLeft.Sprite,
				> 0 => ModEntry.Instance.SweepRight.Sprite,
				_ => ModEntry.Instance.Swoop.Sprite,
			},
			number = swoopDir == 0 ? __instance.damage : Math.Abs(swoopDir),
			color = Colors.redd,
		};
	}
	
	private static bool ASpawn_Begin_Prefix(ASpawn __instance, G g, Combat c)
	{
		if (__instance.thing is not MavisStuff)
			return true;
		if (__instance.fromPlayer && g.state.ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return true;
		
		return !c.stuff.Values.Any(@object => @object is MavisStuff);
	}
	
	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not MavisSwoopAction swoopAction)
			return true;

		var attack = Mutil.DeepCopy(swoopAction.Attack);
		ModEntry.Instance.Helper.ModData.SetModData(attack, "MavisSwoopDir", swoopAction.Direction);
		__result = Card.RenderAction(g, state, attack, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		return false;
	}
}

internal sealed class MavisStuff : StuffBase
{
	public override bool IsHostile()
		=> this.targetPlayer;

	public override Spr? GetIcon()
		=> ModEntry.Instance.BatIcon.Sprite;

	public override double GetWiggleAmount()
		=> 1;

	public override double GetWiggleRate()
		=> 5;

	public override void Render(G g, Vec v)
	{
		base.Render(g, v);
		DrawWithHilight(g, ModEntry.Instance.BatSprite.Sprite, v + GetOffset(g, doRound: false), flipY: targetPlayer);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new MavisSwoopAction { Attack = new() { damage = 1 } }];

	public override List<Tooltip> GetTooltips()
		=> [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Midrow::Mavis")
			{
				Icon = GetIcon(),
				TitleColor = Colors.midrow,
				Title = ModEntry.Instance.Localizations.Localize(["midrow", "Mavis", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["midrow", "Mavis", "description"])
			},
			.. StatusMeta.GetTooltips(ModEntry.Instance.MavisCharacter.MissingStatus.Status, 1),
		];

	public override List<CardAction> GetActionsOnDestroyed(State s, Combat c, bool wasPlayer, int worldX)
		=> fromPlayer && s.EnumerateAllArtifacts().FirstOrDefault(a => a is CrimsonTetherArtifact) is { } artifact ? [
			new AStatus { targetPlayer = true, mode = AStatusMode.Set, status = ModEntry.Instance.MavisCharacter.MissingStatus.Status, statusAmount = 0, artifactPulse = artifact.Key() },
			new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
		] : [];
}

internal sealed class MavisSwoopAction : CardAction
{
	public required AAttack Attack;
	public int Direction;

	public override Icon? GetIcon(State s)
		=> new();

	public override List<Tooltip> GetTooltips(State s)
		=> [
			Direction switch
			{
				< 0 => new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Action::SweepLeft")
				{
					Icon = ModEntry.Instance.SweepLeft.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "sweep", "left", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "sweep", "left", "description"]),
					vals = [Math.Abs(Direction)],
				},
				> 0 => new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Action::SweepRight")
				{
					Icon = ModEntry.Instance.SweepRight.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "sweep", "right", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "sweep", "right", "description"]),
					vals = [Math.Abs(Direction)],
				},
				_ => new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Action::Swoop")
				{
					Icon = ModEntry.Instance.Swoop.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "swoop", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "swoop", "description"]),
					vals = [Attack.damage],
				},
			},
			.. new MavisStuff().GetTooltips(),
			.. Attack.GetTooltips(s)
				.Where(tooltip => tooltip is not TTGlossary { key: "action.attack.name" })
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (c.stuff.FirstOrNull(kvp => kvp.Value is MavisStuff) is not { } kvp)
		{
			timer = 0;
			return;
		}

		if (Direction == 0)
		{
			var attack = Mutil.DeepCopy(Attack);
			attack.fromDroneX = kvp.Key;
			c.QueueImmediate(attack);
			return;
		}

		var sign = Math.Sign(Direction);
		c.QueueImmediate([
			new AKickMiette { x = kvp.Key, dir = sign },
			new MavisSwoopAction { Attack = Mutil.DeepCopy(Attack), Direction = Direction - sign },
		]);
	}
}