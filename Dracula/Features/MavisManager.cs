using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Midrow::Bat")
			{
				Icon = GetIcon(),
				TitleColor = Colors.midrow,
				Title = ModEntry.Instance.Localizations.Localize(["midrow", "Bat", "Normal", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["midrow", "Bat", "Normal", "description"])
			},
		];
}

internal sealed class MavisSwoopAction : CardAction
{
	public required AAttack Attack;
	public int Direction;

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