using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CatExpansion;

internal sealed class ExeOfferingDistribution : IRegisterable
{
	private static State? LastState;
	private static HashSet<Type>? CurrentProcessExeCardTypes;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetOffering)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: typeof(CardReward).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<GetOffering>") && m.Name.EndsWith('1') && m.ReturnType == typeof(bool)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Where_Postfix))
		);
	}

	private static void CardReward_GetOffering_Prefix(State s)
	{
		LastState = s;
		CurrentProcessExeCardTypes = null;
	}

	private static void CardReward_GetOffering_Where_Postfix(Card c, ref bool __result)
	{
		if (!__result)
			return;
		if (LastState is not { } state)
			return;
		if (!ModEntry.Instance.EssentialsApi.IsExeCardType(c.GetType()))
			return;

		if (CurrentProcessExeCardTypes is null)
		{
			var newExeCardTypes = new HashSet<Type> { typeof(ColorlessCATSummon) };
			var amountLeft = ModEntry.Instance.Settings.ProfileBased.Current.TotalExeDistribution;

			if (amountLeft > 0 && ModEntry.Instance.Settings.ProfileBased.Current.EnforceExesFromCurrentCrew)
			{
				var crewExeCardTypes = state.characters
					.Where(character => character.deckType is not null)
					.Where(character => !ModEntry.Instance.EssentialsApi.IsBlacklistedExeOffering(character.deckType!.Value))
					.Select(character => (DeckType: character.deckType!.Value, ExeCardType: ModEntry.Instance.EssentialsApi.GetExeCardTypeForDeck(character.deckType!.Value)))
					.Where(e => e.ExeCardType is not null && e.ExeCardType != typeof(ColorlessCATSummon))
					.Select(e => (DeckType: e.DeckType, ExeCardType: e.ExeCardType!))
					.ToList();

				if (crewExeCardTypes.Count > amountLeft)
					crewExeCardTypes = crewExeCardTypes
						.Shuffle(state.rngCardOfferings)
						.Take(amountLeft)
						.ToList();

				foreach (var entry in crewExeCardTypes)
					newExeCardTypes.Add(entry.ExeCardType);
				amountLeft -= crewExeCardTypes.Count;
			}

			if (amountLeft > 0)
			{
				var remainingExeCardTypes = NewRunOptions.allChars
					.Where(deck => !ModEntry.Instance.EssentialsApi.IsBlacklistedExeOffering(deck))
					.Select(deck => (DeckType: deck, ExeCardType: ModEntry.Instance.EssentialsApi.GetExeCardTypeForDeck(deck)))
					.Where(e => e.ExeCardType is not null && e.ExeCardType != typeof(ColorlessCATSummon))
					.Select(e => (DeckType: e.DeckType, ExeCardType: e.ExeCardType!))
					.Where(e => !newExeCardTypes.Contains(e.ExeCardType))
					.ToList();
				
				if (remainingExeCardTypes.Count > amountLeft)
					remainingExeCardTypes = remainingExeCardTypes
						.Shuffle(state.rngCardOfferings)
						.Take(amountLeft)
						.ToList();
				
				foreach (var entry in remainingExeCardTypes)
					newExeCardTypes.Add(entry.ExeCardType);
				// amountLeft -= remainingExeCardTypes.Count;
			}

			CurrentProcessExeCardTypes = newExeCardTypes;
		}

		if (!CurrentProcessExeCardTypes.Contains(c.GetType()))
			__result = false;
	}
}