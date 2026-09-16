using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class Abilities : IRegisterable
{
	public static ICardTraitEntry AbilityTrait { get; private set; } = null!;
	public static ICardTraitEntry CooldownTrait { get; private set; } = null!;

	private static ISpriteEntry BaseAbilityIcon = null!;
	private static ISpriteEntry BaseCooldownIcon = null!;
	private static readonly Dictionary<int, Spr> AbilityIcons = [];
	private static readonly Dictionary<int, Spr> CooldownIcons = [];
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
			
			if (GetCooldown(state, state.route as Combat ?? DB.fakeCombat, args.Card) <= 0)
				return;

			args.SetOverride(AbilityTrait, true);
			if (GetCurrentCooldown(state, state.route as Combat ?? DB.fakeCombat, args.Card) > 0)
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
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnEnd), (Combat combat) =>
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
	}
	
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