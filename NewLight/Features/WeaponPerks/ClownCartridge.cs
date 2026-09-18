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

internal sealed class ClownCartridgeWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/ClownCartridge.png"));

		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("ClownCartridge", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "ClownCartridge", "Name"]).Localize,
			Tooltips = (_, card) =>
			{
				string description;
				if (card is null)
					description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ClownCartridge", "Description", "Stateless"]);
				else if (Ammo.GetBaseSpecialCost(card) > 1 || Ammo.GetBaseHeavyCost(card) > 1)
					description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ClownCartridge", "Description", "Ammo"]);
				else
					description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ClownCartridge", "Description", "Energy"]);
				
				return [
					new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::ClownCartridge")
					{
						Icon = icon.Sprite,
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ClownCartridge", "Name"]),
						Description = description,
					}
				];
			}
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is WeaponCard.IUsesAmmo and not (WeaponCard.IUsesAmmo.IOnlyOne and WeaponCard.IEnergyFree);
		
		Ammo.Instance.Register(new AmmoHook(), 0);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetCurrentCost)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetCurrentCost_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActualDamage)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActualDamage_Transpiler))
		);
	}

	private static void Card_GetCurrentCost_Postfix(Card __instance, State s, ref int __result)
	{
		if (s.route is not Combat combat)
			return;

		var specialCost = Ammo.GetSpecialCost(s, combat, __instance);
		var heavyCost = Ammo.GetHeavyCost(s, combat, __instance);
		if (specialCost > 1 || heavyCost > 1)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;

		__result = Math.Max(__result - 1, 0);
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
		if (s.route is not Combat)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Trait))
			return;
		actualDamage--;
	}

	private sealed class AmmoHook : Ammo.IHook
	{
		public void ModifySpecialAmmoCost(State state, Combat combat, Card card, ref int? cost)
		{
			if (cost is null or <= 1)
				return;
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;

			cost--;
		}
		
		public void ModifyHeavyAmmoCost(State state, Combat combat, Card card, ref int? cost)
		{
			if (cost is null or <= 1)
				return;
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;

			cost--;
		}
	}
}