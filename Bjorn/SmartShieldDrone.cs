using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Bjorn;

internal sealed class SmartShieldDrone : ShieldDrone, IRegisterable
{
	private static ISpriteEntry DroneSprite = null!;
	private static ISpriteEntry DroneIcon = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		DroneSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/SmartShieldDrone.png"));
		DroneIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/SmartShieldDroneIcon.png"));

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler))
		);
	}

	public override void Render(G g, Vec v)
		=> DrawWithHilight(g, DroneSprite.Sprite, v + GetOffset(g), flipY: targetPlayer);

	public override Spr? GetIcon()
		=> DroneIcon.Sprite;

	public override List<Tooltip> GetTooltips()
		=> [
			new GlossaryTooltip($"midrow.{ModEntry.Instance.Package.Manifest.UniqueName}::SmartShieldDrone")
			{
				Icon = DroneIcon.Sprite,
				TitleColor = Colors.midrow,
				Title = ModEntry.Instance.Localizations.Localize(["midrow", "SmartShieldDrone", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["midrow", "SmartShieldDrone", "description"]),
			},
			.. new SmartShieldAction { Amount = 1 }.GetTooltips(DB.fakeState),
			.. (bubbleShield ? [new TTGlossary("midrow.bubbleShield")] : Array.Empty<Tooltip>())
		];

	public override List<CardAction>? GetActions(State s, Combat c)
	{
		var attack = new AAttack
		{
			isBeam = true,
			fromDroneX = x,
			targetPlayer = targetPlayer,
			damage = 0,
		};
		ModEntry.Instance.Helper.ModData.SetModData(attack, "SmartShield", 1);
		return [attack];
	}

	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0).ExtractLabels(out var labels),
					ILMatches.Ldflda("status"),
					ILMatches.Call("get_HasValue"),
					ILMatches.Brfalse
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler_GrantSmartShield)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void AAttack_Begin_Transpiler_GrantSmartShield(AAttack attack, State state, Combat combat)
	{
		var amount = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(attack, "SmartShield");
		if (amount <= 0)
			return;

		combat.QueueImmediate(new SmartShieldAction { TargetPlayer = attack.targetPlayer, Amount = amount });
	}
}
