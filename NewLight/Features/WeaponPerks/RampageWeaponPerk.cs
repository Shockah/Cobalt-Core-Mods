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
using Shockah.Kokoro;

namespace Shockah.NewLight;

internal sealed class RampageWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Rampage.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Rampage", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Rampage", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Rampage")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Rampage", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Rampage", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.DamageWeaponPerks.Add(Trait.UniqueName);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActualDamage)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActualDamage_Transpiler))
		);
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
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Trait))
			return;
		
		actualDamage += ModEntry.Instance.KokoroApi.TimesPlayed.GetTimesPlayed(card, IKokoroApi.IV2.ITimesPlayedApi.Interval.Turn);
	}
}