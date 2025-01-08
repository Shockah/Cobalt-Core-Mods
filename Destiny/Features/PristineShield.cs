using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Destiny;

internal sealed class PristineShield : IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook
{
	internal static IStatusEntry PristineShieldStatus { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		PristineShieldStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("PristineShield", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/PristineShield.png")).Sprite,
				color = new Color("FF6FEC"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "PristineShield", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "PristineShield", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Transpiler))
		);

		var instance = new PristineShield();
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(instance);
	}
	
	private static IEnumerable<CodeInstruction> Ship_DirectHullDamage_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.LdcI4((int)Status.perfectShield),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble.GetBranchTarget(out var branchTarget),
					ILMatches.Instruction(OpCodes.Ret),
				])
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.CreateLabel(il, out var successLabel)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Transpiler_HandlePristineShield))),
					new CodeInstruction(OpCodes.Brtrue, successLabel),
					new CodeInstruction(OpCodes.Ret),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool Ship_DirectHullDamage_Transpiler_HandlePristineShield(Ship ship)
	{
		if (ship.Get(PristineShieldStatus.Status) <= 0)
			return true;

		ship.Add(PristineShieldStatus.Status, -1);
		return false;
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status != PristineShieldStatus.Status)
			return false;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;
		if (args.Amount == 0)
			return false;

		args.Amount = Math.Max(args.Amount - 1, 0);
		return false;
	}
}