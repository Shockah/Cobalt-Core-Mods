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

namespace Shockah.NewLight;

internal abstract class ExoticWeaponCard : WeaponCard, IRegisterable
{
	public const Rarity RARITY = Rarity.rare;
	
	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetOffering)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> CardReward_GetOffering_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.AnyLdloc,
					ILMatches.Ldarg(2),
					ILMatches.Stloc<Deck?>(originalMethod).GetLocalIndex(out var deckLocalIndex),
					ILMatches.Ldloca<Deck?>(originalMethod),
					ILMatches.Call("get_HasValue"),
				])
				.Find(ILMatches.Ldsfld(nameof(DB.releasedCards)))
				.Find(ILMatches.Stloc<List<Card>>(originalMethod))
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldloc, deckLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Transpiler_ModifyValidCards))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static List<Card> CardReward_GetOffering_Transpiler_ModifyValidCards(List<Card> validCards, State state, Deck? deck)
	{
		if (deck != ModEntry.Instance.GuardianDeck.Deck)
			return validCards;

		var ownedExoticWeapons = state.GetAllCards()
			.Where(card => card is ExoticWeaponCard)
			.Select(card => card.Key())
			.ToHashSet();

		validCards
			.RemoveAll(card => ownedExoticWeapons.Contains(card.Key()));
		
		return validCards;
	}
}