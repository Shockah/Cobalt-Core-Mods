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

namespace Shockah.NewLight;

internal sealed class FrenzyWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Frenzy.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Frenzy", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Frenzy", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Frenzy")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Frenzy", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Frenzy", "Description"]),
				}
			]
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAfterPlayerTurn), nameof(AAfterPlayerTurn.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAfterPlayerTurn_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStartPlayerTurn), nameof(AStartPlayerTurn.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStartPlayerTurn_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_DirectHullDamage_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActualDamage)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActualDamage_Transpiler))
		);
	}

	private static void AAfterPlayerTurn_Begin_Postfix(Combat c)
		=> c.PlayerDidDamageLastTurn = c.PlayerDidDamageThisTurn;

	private static void AStartPlayerTurn_Begin_Postfix(Combat c)
		=> c.PlayerDidDamageThisTurn = false;
	
	private static void Ship_NormalDamage_Prefix(Ship __instance, out (int Hull, int Shield, int TempShield) __state)
		=> __state = (__instance.hull, __instance.Get(Status.shield), __instance.Get(Status.tempShield));

	private static void Ship_NormalDamage_Postfix(Ship __instance, Combat c, ref (int Hull, int Shield, int TempShield) __state)
	{
		if (__instance != c.otherShip)
			return;
		if (__state.Hull - __instance.hull <= 0 && __state.Shield - __instance.Get(Status.shield) <= 0 && __state.TempShield - __instance.Get(Status.tempShield) <= 0)
			return;

		c.PlayerDidDamageThisTurn = true;
	}
	
	private static void Ship_DirectHullDamage_Prefix(Ship __instance, out (int Hull, int Shield, int TempShield) __state)
		=> __state = (__instance.hull, __instance.Get(Status.shield), __instance.Get(Status.tempShield));

	private static void Ship_DirectHullDamage_Postfix(Ship __instance, Combat c, ref (int Hull, int Shield, int TempShield) __state)
	{
		if (__instance != c.otherShip)
			return;
		if (__state.Hull - __instance.hull <= 0 && __state.Shield - __instance.Get(Status.shield) <= 0 && __state.TempShield - __instance.Get(Status.tempShield) <= 0)
			return;

		c.PlayerDidDamageThisTurn = true;
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_GetActualDamage_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<int>(originalMethod),
					ILMatches.Ldloc<Ship>(originalMethod),
					ILMatches.LdcI4(Status.powerdrive),
					ILMatches.Call(nameof(Ship.Get)),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod).GetLocalIndex(out var actualDamageLocalIndex),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloca, actualDamageLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActualDamage_Transpiler_ModifyActualDamage))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Card_GetActualDamage_Transpiler_ModifyActualDamage(State s, Card? card, ref int actualDamage)
	{
		if (card is null)
			return;
		if (s.route is not Combat combat)
			return;
		if (!combat.PlayerDidDamageLastTurn)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Trait))
			return;
		actualDamage++;
	}
}

file static class FrenzyCardTraitExt
{
	extension(Combat combat)
	{
		public bool PlayerDidDamageThisTurn
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(combat, "PlayerDidDamageThisTurn");
			set => ModEntry.Instance.Helper.ModData.SetModData(combat, "PlayerDidDamageThisTurn", value);
		}
		
		public bool PlayerDidDamageLastTurn
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(combat, "PlayerDidDamageLastTurn");
			set => ModEntry.Instance.Helper.ModData.SetModData(combat, "PlayerDidDamageLastTurn", value);
		}
	}
}