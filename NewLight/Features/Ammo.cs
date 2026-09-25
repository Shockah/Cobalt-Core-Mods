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
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.NewLight;

internal sealed class Ammo : HookManager<Ammo.IHook>, IRegisterable
{
	public interface IHook
	{
		void ModifyMaxSpecialAmmo(ref ModifyMaxAmmoArgs args) { }
		void ModifyMaxHeavyAmmo(ref ModifyMaxAmmoArgs args) { }
		void ModifySpecialAmmoCost(ref ModifyAmmoCostArgs args) { }
		void ModifyHeavyAmmoCost(ref ModifyAmmoCostArgs args) { }
		
		public struct ModifyMaxAmmoArgs
		{
			public required State State { get; init; }
			public required Combat Combat { get; init; }
			public required Ship Ship { get; init; }
			public required int BaseAmmo { get; init; }
			public required int Ammo { get; set; }
		}
		
		public struct ModifyAmmoCostArgs
		{
			public required State State { get; init; }
			public required Combat Combat { get; init; }
			public required Card Card { get; init; }
			public required int? BaseCost { get; init; }
			public required int? Cost { get; set; }
		}
	}

	public const int BASE_MAX_SPECIAL_AMMO = 5;
	public const int BASE_MAX_HEAVY_AMMO = 5;
	
	internal static readonly Ammo Instance = new();

	private Ammo() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}

	internal static IStatusEntry SpecialStatus { get; private set; } = null!;
	internal static IStatusEntry HeavyStatus { get; private set; } = null!;
	
	internal static ISpriteEntry SpecialCostIcon { get; private set; } = null!;
	internal static ISpriteEntry HeavyCostIcon { get; private set; } = null!;
	
	private static readonly Dictionary<string, Dictionary<Upgrade, int>> BaseSpecialCost = [];
	private static readonly Dictionary<string, Dictionary<Upgrade, int>> BaseHeavyCost = [];
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		SpecialStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("SpecialAmmo", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/SpecialAmmo.png")).Sprite,
				color = new("7AF48B"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Status", "SpecialAmmo", "Name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["Status", "SpecialAmmo", "Description"]).Localize,
		});
		HeavyStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("HeavyAmmo", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/HeavyAmmo.png")).Sprite,
				color = new("B286FF"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Status", "HeavyAmmo", "Name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["Status", "HeavyAmmo", "Description"]).Localize,
		});

		SpecialCostIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/UI/SpecialAmmoCost.png"));
		HeavyCostIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/UI/HeavyAmmoCost.png"));
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatStart), (State state, Combat combat) =>
		{
			var allCards = state.GetAllCards().ToList();
			var specialAmmoCards = allCards.Count(card => GetSpecialCost(state, combat, card) is not null);
			var heavyAmmoCards = allCards.Count(card => GetHeavyCost(state, combat, card) is not null);

			if (specialAmmoCards != 0)
				state.ship.Add(SpecialStatus.Status, specialAmmoCards);
			if (heavyAmmoCards != 0)
				state.ship.Add(HeavyStatus.Status, heavyAmmoCards);
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);

		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(new StatusLogicHook());
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new StatusRenderingHook());
	}

	public static int GetMaxSpecialAmmo(State state, Combat combat, Ship ship)
	{
		var args = new IHook.ModifyMaxAmmoArgs
		{
			State = state,
			Combat = combat,
			Ship = ship,
			BaseAmmo = BASE_MAX_SPECIAL_AMMO,
			Ammo = BASE_MAX_SPECIAL_AMMO,
		};
		foreach (var hook in Instance)
			hook.ModifyMaxSpecialAmmo(ref args);

		return args.Ammo;
	}

	public static int GetMaxHeavyAmmo(State state, Combat combat, Ship ship)
	{
		var args = new IHook.ModifyMaxAmmoArgs
		{
			State = state,
			Combat = combat,
			Ship = ship,
			BaseAmmo = BASE_MAX_HEAVY_AMMO,
			Ammo = BASE_MAX_HEAVY_AMMO,
		};
		foreach (var hook in Instance)
			hook.ModifyMaxHeavyAmmo(ref args);

		return args.Ammo;
	}

	public static int? GetBaseSpecialCost(Card card)
		=> GetBaseSpecialCost(card.Key(), card.upgrade);

	public static int? GetBaseHeavyCost(Card card)
		=> GetBaseHeavyCost(card.Key(), card.upgrade);

	public static int? GetBaseSpecialCost(string key, Upgrade upgrade)
	{
		if (!BaseSpecialCost.TryGetValue(key, out var perUpgrade))
			return null;
		if (!perUpgrade.TryGetValue(upgrade, out var value))
			return null;
		return value;
	}

	public static int? GetBaseHeavyCost(string key, Upgrade upgrade)
	{
		if (!BaseHeavyCost.TryGetValue(key, out var perUpgrade))
			return null;
		if (!perUpgrade.TryGetValue(upgrade, out var value))
			return null;
		return value;
	}

	public static int? GetSpecialCost(State state, Combat combat, Card card)
	{
		var cost = GetBaseSpecialCost(card);

		var args = new IHook.ModifyAmmoCostArgs
		{
			State = state,
			Combat = combat,
			Card = card,
			BaseCost = cost,
			Cost = cost,
		};
		foreach (var hook in Instance)
			hook.ModifySpecialAmmoCost(ref args);

		return args.Cost;
	}

	public static int? GetHeavyCost(State state, Combat combat, Card card)
	{
		var cost = GetBaseHeavyCost(card);
		
		var args = new IHook.ModifyAmmoCostArgs
		{
			State = state,
			Combat = combat,
			Card = card,
			BaseCost = cost,
			Cost = cost,
		};
		foreach (var hook in Instance)
			hook.ModifyHeavyAmmoCost(ref args);

		return args.Cost;
	}

	public static void SetBaseHeavyCost(string key, int? value)
	{
		SetBaseHeavyCost(key, Upgrade.None, value);
		SetBaseHeavyCost(key, Upgrade.A, value);
		SetBaseHeavyCost(key, Upgrade.B, value);
	}

	public static void SetBaseSpecialCost(string key, int? value)
	{
		SetBaseSpecialCost(key, Upgrade.None, value);
		SetBaseSpecialCost(key, Upgrade.A, value);
		SetBaseSpecialCost(key, Upgrade.B, value);
	}

	public static void SetBaseSpecialCost(string key, Upgrade upgrade, int? value)
	{
		if (value is null)
		{
			if (!BaseSpecialCost.TryGetValue(key, out var perUpgrade))
				return;
			
			perUpgrade.Remove(upgrade);
			if (perUpgrade.Count == 0)
				BaseSpecialCost.Remove(key);
		}
		else
		{
			ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(BaseSpecialCost, key, out var perUpgradeExists);
			if (!perUpgradeExists)
				perUpgrade = [];
			perUpgrade![upgrade] = value.Value;
		}
	}

	public static void SetBaseHeavyCost(string key, Upgrade upgrade, int? value)
	{
		if (value is null)
		{
			if (!BaseHeavyCost.TryGetValue(key, out var perUpgrade))
				return;
			
			perUpgrade.Remove(upgrade);
			if (perUpgrade.Count == 0)
				BaseHeavyCost.Remove(key);
		}
		else
		{
			ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(BaseHeavyCost, key, out var perUpgradeExists);
			if (!perUpgradeExists)
				perUpgrade = [];
			perUpgrade![upgrade] = value.Value;
		}
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_GetAllTooltips_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Newobj(AccessTools.DeclaredConstructor(typeof(List<Tooltip>), [])),
					ILMatches.Stloc<List<Tooltip>>(originalMethod).GetLocalIndex(out var tooltipsLocalIndex),
				])
				.Find([
					ILMatches.Ldarg(3),
					ILMatches.Brfalse,
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld(nameof(CardData.unplayable)),
					ILMatches.Brfalse.GetBranchTarget(out var pastUnplayableLabel),
				])
				.PointerMatcher(pastUnplayableLabel)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldloc, tooltipsLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Transpiler_AmmoCostTooltips))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Card_GetAllTooltips_Transpiler_AmmoCostTooltips(Card card, State state, List<Tooltip> tooltips)
	{
		var combat = state.route as Combat ?? DB.fakeCombat;

		if (GetSpecialCost(state, combat, card) is { } specialCost)
			tooltips.Add(new GlossaryTooltip($"keyword.{ModEntry.Instance.Package.Manifest.UniqueName}::SpecialAmmoCost")
			{
				Icon = SpecialStatus.Configuration.Definition.icon,
				TitleColor = Colors.keyword,
				Title = ModEntry.Instance.Localizations.Localize(["Status", "SpecialAmmo", "CostTooltip", "Name"]),
				Description = ModEntry.Instance.Localizations.Localize(["Status", "SpecialAmmo", "CostTooltip", "Description"], new { Amount = specialCost }),
			});
		if (GetHeavyCost(state, combat, card) is { } heavyCost)
			tooltips.Add(new GlossaryTooltip($"keyword.{ModEntry.Instance.Package.Manifest.UniqueName}::HeavyAmmoCost")
			{
				Icon = HeavyStatus.Configuration.Definition.icon,
				TitleColor = Colors.keyword,
				Title = ModEntry.Instance.Localizations.Localize(["Status", "HeavyAmmo", "CostTooltip", "Name"]),
				Description = ModEntry.Instance.Localizations.Localize(["Status", "HeavyAmmo", "CostTooltip", "Description"], new { Amount = heavyCost }),
			});
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Isinst<YellowCardTrash>(),
					ILMatches.Brtrue.GetBranchTarget(out var pastCostRenderingLabel),
				])
				.PointerMatcher(pastCostRenderingLabel)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldarg, 10),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler_RenderAmmoCost))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void Card_Render_Transpiler_RenderAmmoCost(Card card, G g, State? fakeState, UIKey? keyOverride)
	{
		var state = fakeState ?? g.state;
		var key = keyOverride ?? card.UIKey();
		if (g.boxes.LastOrDefault(b => b.key == key) is not { } box)
			return;

		var position = box.rect.xy + card.GetShakeOffset(g);

		var color = Color.Lerp(Colors.white, Colors.redd, card.shakeNoAnim);
		var ammoIndex = 0;
		var heavyAmmoCost = GetHeavyCost(state, (state.route as Combat) ?? DB.fakeCombat, card);
		var specialAmmoCost = GetSpecialCost(state, (state.route as Combat) ?? DB.fakeCombat, card);

		for (var i = 0; i < heavyAmmoCost; i++)
			Draw.Sprite(HeavyCostIcon.Sprite, position.x + 12 + (ammoIndex++) * 2, position.y + 19, color: color);
		for (var i = 0; i < specialAmmoCost; i++)
			Draw.Sprite(SpecialCostIcon.Sprite, position.x + 12 + (ammoIndex++) * 2, position.y + 19, color: color);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<int>(originalMethod),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(Combat.energy)),
					ILMatches.Bgt.GetBranchTarget(out var cantAffordLabel),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_CanAffordAmmo))),
					new CodeInstruction(OpCodes.Brfalse, cantAffordLabel.Value),
				])
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(Combat.energy)),
					ILMatches.Ldloc<int>(originalMethod),
					ILMatches.Instruction(OpCodes.Sub),
					ILMatches.Stfld(nameof(Combat.energy)),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_SpendAmmo))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static bool Combat_TryPlayCard_Transpiler_CanAffordAmmo(State state, Combat combat, Card card, bool playNoMatterWhatForFree)
	{
		if (playNoMatterWhatForFree)
			return true;

		var specialAmmoCost = GetSpecialCost(state, combat, card);
		if (specialAmmoCost > state.ship.Get(SpecialStatus.Status))
			return false;
		
		var heavyAmmoCost = GetHeavyCost(state, combat, card);
		if (heavyAmmoCost > state.ship.Get(HeavyStatus.Status))
			return false;
		
		return true;
	}

	private static void Combat_TryPlayCard_Transpiler_SpendAmmo(State state, Combat combat, Card card, bool playNoMatterWhatForFree)
	{
		if (playNoMatterWhatForFree)
			return;
		
		if (GetSpecialCost(state, combat, card) is { } specialCost)
			state.ship.Add(SpecialStatus.Status, -specialCost);
		if (GetHeavyCost(state, combat, card) is { } heavyCost)
			state.ship.Add(HeavyStatus.Status, -heavyCost);
	}

	private sealed class StatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
		{
			int maxStatus;
			if (args.Status == SpecialStatus.Status)
				maxStatus = GetMaxSpecialAmmo(args.State, args.Combat, args.Ship);
			else if (args.Status == HeavyStatus.Status)
				maxStatus = GetMaxHeavyAmmo(args.State, args.Combat, args.Ship);
			else
				return args.NewAmount;
			
			return Math.Min(args.NewAmount, maxStatus);
		}
	}

	private sealed class StatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
		{
			int maxStatus;
			if (args.Status == SpecialStatus.Status)
				maxStatus = GetMaxSpecialAmmo(args.State, args.Combat, args.Ship);
			else if (args.Status == HeavyStatus.Status)
				maxStatus = GetMaxHeavyAmmo(args.State, args.Combat, args.Ship);
			else
				return null;
			
			var colors = new Color[maxStatus];
			for (var i = 0; i < colors.Length; i++)
				colors[i] = args.Amount > i ? ModEntry.Instance.KokoroApi.StatusRendering.DefaultActiveStatusBarColor : ModEntry.Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor;
			
			return ModEntry.Instance.KokoroApi.StatusRendering.MakeBarStatusInfoRenderer().SetSegments(colors);
		}
		
		public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		{
			var state = MG.inst.g.state ?? DB.fakeState;
			var combat = state.route as Combat ?? DB.fakeCombat;

			string localizationKey;
			int maxStatus;
			if (args.Status == SpecialStatus.Status)
			{
				localizationKey = "SpecialAmmo";
				maxStatus = GetMaxSpecialAmmo(state, combat, args.Ship ?? DB.fakeState.ship);
			}
			else if (args.Status == HeavyStatus.Status)
			{
				localizationKey = "HeavyAmmo";
				maxStatus = GetMaxHeavyAmmo(state, combat, args.Ship ?? DB.fakeState.ship);
			}
			else
			{
				return args.Tooltips;
			}

			var tooltipList = args.Tooltips.ToList();
			var index = tooltipList.FindIndex(t => t is TTGlossary glossary && glossary.key == $"status.{args.Status}");
			if (index == -1)
				return tooltipList;

			tooltipList[index] = new GlossaryTooltip(((TTGlossary)tooltipList[index]).key)
			{
				Icon = DB.statuses[args.Status].icon,
				TitleColor = Colors.status,
				Title = ModEntry.Instance.Localizations.Localize(["Status", localizationKey, "Name"]),
				Description = ModEntry.Instance.Localizations.Localize(["Status", localizationKey, "Description"], new { Max = maxStatus }),
			};
			return tooltipList;
		}
	}
}