using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class Abilities : IRegisterable
{
	public const string MOVEMENT_ABILITY = "Movement";
	public const string MELEE_ABILITY = "Melee";
	public const string UTILITY_ABILITY = "Utility";
	public const string SUPER_ABILITY = "Super";
	
	public static ICardTraitEntry AbilityTrait { get; private set; } = null!;
	public static ICardTraitEntry CooldownTrait { get; private set; } = null!;

	private static ISpriteEntry BaseAbilityIcon = null!;
	private static ISpriteEntry BaseCooldownIcon = null!;
	private static readonly Dictionary<int, Spr> AbilityIcons = [];
	private static readonly Dictionary<int, Spr> CooldownIcons = [];
	private static readonly Dictionary<string, string> AbilityType = [];
	private static readonly Dictionary<string, Dictionary<Upgrade, int>> CooldownPerUpgrade = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BaseAbilityIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardTraits/Ability.png"));
		BaseCooldownIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardTraits/Cooldown.png"));
		
		CooldownTrait = helper.Content.Cards.RegisterTrait("Cooldown", new()
		{
			Icon = (state, card) =>
			{
				var realState = MG.inst.g.state ?? state;
				return ObtainCooldownIcon(card is null ? 10 : GetCurrentCooldown(realState, realState.route as Combat ?? DB.fakeCombat, card));
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "Cooldown", "Name"]).Localize,
			Tooltips = (state, card) =>
			{
				var realState = MG.inst.g.state ?? state;
				int? cooldown = card is null ? null : GetCurrentCooldown(realState, realState.route as Combat ?? DB.fakeCombat, card);
				var description = cooldown is null
					? ModEntry.Instance.Localizations.Localize(["CardTrait", "Cooldown", "Description", "WithoutCard"])
					: ModEntry.Instance.Localizations.Localize(["CardTrait", "Cooldown", "Description", "WithCard"], new { Amount = cooldown.Value });

				return [
					new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Cooldown")
					{
						Icon = ObtainCooldownIcon(cooldown ?? 10),
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "Cooldown", "Name"]),
						Description = description,
					}
				];
			}
		});
		AbilityTrait = helper.Content.Cards.RegisterTrait("Ability", new()
		{
			Icon = (state, card) =>
			{
				var realState = MG.inst.g.state ?? state;
				return ObtainAbilityIcon(card is null ? 10 : GetCooldown(realState, realState.route as Combat ?? DB.fakeCombat, card));
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "Ability", "Name"]).Localize,
			Tooltips = (state, card) =>
			{
				var realState = MG.inst.g.state ?? state;
				int? cooldown = card is null ? null : GetCooldown(realState, realState.route as Combat ?? DB.fakeCombat, card);
				var description = cooldown is null
					? ModEntry.Instance.Localizations.Localize(["CardTrait", "Ability", "Description", "WithoutCard"])
					: ModEntry.Instance.Localizations.Localize(["CardTrait", "Ability", "Description", "WithCard"], new { Amount = cooldown.Value });

				return [
					new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Ability")
					{
						Icon = ObtainAbilityIcon(cooldown ?? 10),
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "Ability", "Name"]),
						Description = description,
					}
				];
			}
		});

		helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, args) =>
		{
			var state = MG.inst.g.state ?? args.State;
			var combat = state.route as Combat ?? DB.fakeCombat;
			
			if (GetCooldown(state, combat, args.Card) <= 0)
				return;

			args.SetOverride(AbilityTrait, true);

			if (!combat.Abilities.Contains(args.Card))
				return;
			if (GetCurrentCooldown(state, combat, args.Card) <= 0)
				return;

			args.SetOverride(CooldownTrait, true);
		};
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnQueueEmptyDuringPlayerTurn), (State state, Combat combat) =>
		{
			if (combat.hand.Count >= 10)
				return;
			if (!combat.Abilities.Any(card => GetCurrentCooldown(state, combat, card) <= 0))
				return;
			if (combat.hand.Any(card => card is AbilitiesCard))
				return;
			
			combat.SendCardToHand(state, new AbilitiesCard());
		});
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnStart), (Combat combat) =>
		{
			var currentCooldown = combat.CurrentCooldown;
			foreach (var card in combat.Abilities)
				currentCooldown[card.uuid] = currentCooldown.GetValueOrDefault(card.uuid) + 1;
		});
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatStart), (State state, Combat combat) =>
		{
			var abilities = combat.Abilities;
			
			for (var i = state.deck.Count - 1; i >= 0; i--)
			{
				var card = state.deck[i];
				if (!helper.Content.Cards.IsCardTraitActive(state, card, AbilityTrait))
					continue;
				state.deck.RemoveAt(i);
				abilities.Add(card);
			}
		}, 1_000_000);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.ReturnCardsToDeck)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_ReturnCardsToDeck_Prefix_AlmostFirst)), priority: Priority.First - 1)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToDiscard)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToDiscard_Postfix_AlmostLast)), priority: Priority.Last + 1)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetMergedDeckForDisplay)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetMergedDeckForDisplay_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.RemoveCardFromWhereverItIs)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_RemoveCardFromWhereverItIs_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetOffering)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Transpiler))
		);
	}

	public static void SetAbilityType(string key, string? value)
	{
		if (value is null)
			AbilityType.Remove(key);
		else
			AbilityType[key] = value;
	}

	public static string? GetAbilityType(string key)
		=> AbilityType.GetValueOrDefault(key);
	
	public static void SetBaseCooldown(string key, int value)
	{
		SetBaseCooldown(key, Upgrade.None, value);
		SetBaseCooldown(key, Upgrade.A, value);
		SetBaseCooldown(key, Upgrade.B, value);
	}
	
	public static void SetBaseCooldown(string key, Upgrade upgrade, int value)
	{
		ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(CooldownPerUpgrade, key, out var perUpgradeExists);
		if (!perUpgradeExists)
			perUpgrade = [];
		perUpgrade![upgrade] = value;
	}

	public static int GetBaseCooldown(Card card)
		=> GetBaseCooldown(card.Key(), card.upgrade);

	public static int GetBaseCooldown(string key, Upgrade upgrade)
		=> CooldownPerUpgrade.TryGetValue(key, out var perUpgrade) ? perUpgrade.GetValueOrDefault(upgrade) : 0;

	public static int GetCooldown(State state, Combat combat, Card card)
	{
		var cost = GetBaseCooldown(card);
		
		// foreach (var hook in Instance)
		// 	hook.ModifySpecialAmmoCost(state, combat, card, ref cost);

		return cost;
	}

	public static int GetCurrentCooldown(State state, Combat combat, Card card)
		=> Math.Max(GetCooldown(state, combat, card) - combat.CurrentCooldown.GetValueOrDefault(card.uuid), 0);

	public static void SetCurrentCooldown(State state, Combat combat, Card card, int value)
	{
		var cooldown = GetCooldown(state, combat, card);
		combat.CurrentCooldown[value] = cooldown - value;
	}

	private static Spr ObtainAbilityIcon(int amount)
	{
		amount = Math.Clamp(amount, 0, 10);
		if (AbilityIcons.TryGetValue(amount, out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Ability{amount}", () =>
		{
			var baseIcon = SpriteLoader.Get(BaseAbilityIcon.Sprite)!;
			return TextureUtils.CreateTexture(new(baseIcon.Width, baseIcon.Height)
			{
				Actions = _ =>
				{
					Draw.Sprite(baseIcon, 0, 0);

					var text = amount > 9 ? "+" : amount.ToString();
					var textRect = Draw.Text(text, 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
					Draw.Text(text, baseIcon.Width - textRect.w, baseIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
				},
			});
		}).Sprite;

		AbilityIcons[amount] = icon;
		return icon;
	}

	private static Spr ObtainCooldownIcon(int amount)
	{
		amount = Math.Clamp(amount, 0, 10);
		if (CooldownIcons.TryGetValue(amount, out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Cooldown{amount}", () =>
		{
			var baseIcon = SpriteLoader.Get(BaseCooldownIcon.Sprite)!;
			return TextureUtils.CreateTexture(new(baseIcon.Width, baseIcon.Height)
			{
				Actions = _ =>
				{
					Draw.Sprite(baseIcon, 0, 0);

					var text = amount > 9 ? "+" : amount.ToString();
					var textRect = Draw.Text(text, 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
					Draw.Text(text, baseIcon.Width - textRect.w, baseIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
				},
			});
		}).Sprite;

		CooldownIcons[amount] = icon;
		return icon;
	}

	private static void Combat_ReturnCardsToDeck_Prefix_AlmostFirst(Combat __instance, State state)
	{
		foreach (var card in __instance.Abilities)
			state.SendCardToDeck(card);
		__instance.Abilities.Clear();
	}

	private static void Combat_SendCardToDiscard_Postfix_AlmostLast(Combat __instance, State s, Card card)
	{
		var cooldown = GetCooldown(s, __instance, card);
		if (cooldown <= 0)
			return;

		s.RemoveCardFromWhereverItIs(card.uuid);
		__instance.Abilities.Add(card);
		__instance.CurrentCooldown[card.uuid] = 0;
	}

	private static void CardBrowse_GetMergedDeckForDisplay_Postfix(G g, bool includeTemporaryCards, ref List<Card> __result)
	{
		if (g.state.route is not Combat combat)
			return;
		__result.AddRange(combat.Abilities.Where(card => includeTemporaryCards || CardBrowse.IsNotTemporary(card)));
	}

	private static void State_RemoveCardFromWhereverItIs_Postfix(State __instance, int uuid)
	{
		if (__instance.route is not Combat combat)
			return;
		
		var abilities = combat.Abilities;
		for (var i = 0; i < abilities.Count; i++)
		{
			if (abilities[i].uuid != uuid)
				continue;
			abilities.RemoveAt(i);
			break;
		}
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

		var possibleAbilitiesEnumerable = state.GetAllCards();
		if (state.route is Combat combat)
			possibleAbilitiesEnumerable = possibleAbilitiesEnumerable.Concat(combat.Abilities);

		var ownedAbilityTypes = possibleAbilitiesEnumerable
			.Select(card => GetAbilityType(card.Key()))
			.WhereNotNull()
			.ToHashSet();

		validCards
			.RemoveAll(card =>
			{
				var abilityType = GetAbilityType(card.Key());
				return abilityType is not null && ownedAbilityTypes.Contains(abilityType);
			});
		
		return validCards;
	}
}

internal static class AbilitiesExt
{
	extension(Combat combat)
	{
		public List<Card> Abilities
			=> ModEntry.Instance.Helper.ModData.ObtainModData<List<Card>>(combat, "Abilities");
		
		public Dictionary<int, int> CurrentCooldown
			=> ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<int, int>>(combat, "CurrentCooldown");
	}
}