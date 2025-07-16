using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.Gary;

internal sealed class CardDroneManager : IRegisterable
{
	private static ISpriteEntry DroneSprite = null!;
	private static ISpriteEntry DroneIcon = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		DroneSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Midrow/CardDrone.png"));
		DroneIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Midrow/CardDroneIcon.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0).ExtractLabels(out var labels),
					ILMatches.Ldflda("status"),
					ILMatches.Call("get_HasValue"),
					ILMatches.Brfalse
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Transpiler_GrantSmartShield)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void AAttack_Begin_Transpiler_GrantSmartShield(AAttack attack, Combat combat)
	{
		var amount = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(attack, "DrawCards");
		if (amount <= 0)
			return;

		combat.QueueImmediate(new ADrawCard { count = amount });
	}

	internal sealed class Drone : StuffBase
	{
		public override bool IsFriendly()
			=> true;
		
		public override bool IsHostile()
			=> false;

		public override void Render(G g, Vec v)
			=> DrawWithHilight(g, DroneSprite.Sprite, v + GetOffset(g), flipY: true);

		public override Spr? GetIcon()
			=> DroneIcon.Sprite;

		public override List<Tooltip> GetTooltips()
			=> [
				new GlossaryTooltip($"midrow.{ModEntry.Instance.Package.Manifest.UniqueName}::CardDrone")
				{
					Icon = DroneIcon.Sprite,
					TitleColor = Colors.midrow,
					Title = ModEntry.Instance.Localizations.Localize(["midrow", "CardDrone", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["midrow", "CardDrone", "description"]),
				},
				.. (bubbleShield ? [new TTGlossary("midrow.bubbleShield")] : Array.Empty<Tooltip>())
			];

		public override List<CardAction> GetActions(State s, Combat c)
		{
			var attack = new AAttack { isBeam = true, fromDroneX = x, targetPlayer = true, damage = 0 };
			ModEntry.Instance.Helper.ModData.SetModData(attack, "DrawCards", 1);
			return [attack];
		}
	}
}