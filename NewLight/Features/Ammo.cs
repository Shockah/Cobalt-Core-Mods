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

internal sealed class Ammo : HookManager<Ammo.IHook>, IRegisterable
{
	public interface IHook
	{
		void ModifySpecialAmmoCost(State state, Combat combat, Card card, ref int cost) { }
		void ModifyHeavyAmmoCost(State state, Combat combat, Card card, ref int cost) { }
	}

	private Ammo() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}
	
	internal static readonly Ammo Instance = new();

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
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
	}

	public static int GetBaseSpecialCost(Card card)
		=> GetBaseSpecialCost(card.Key(), card.upgrade);

	public static int GetBaseHeavyCost(Card card)
		=> GetBaseHeavyCost(card.Key(), card.upgrade);

	public static int GetBaseSpecialCost(string key, Upgrade upgrade)
		=> BaseSpecialCost.TryGetValue(key, out var perUpgrade) ? perUpgrade.GetValueOrDefault(upgrade) : 0;

	public static int GetBaseHeavyCost(string key, Upgrade upgrade)
		=> BaseHeavyCost.TryGetValue(key, out var perUpgrade) ? perUpgrade.GetValueOrDefault(upgrade) : 0;

	public static int GetSpecialCost(State state, Combat combat, Card card)
	{
		var cost = GetBaseSpecialCost(card);
		
		foreach (var hook in Instance)
			hook.ModifySpecialAmmoCost(state, combat, card, ref cost);

		return cost;
	}

	public static int GetHeavyCost(State state, Combat combat, Card card)
	{
		var cost = GetBaseHeavyCost(card);
		
		foreach (var hook in Instance)
			hook.ModifyHeavyAmmoCost(state, combat, card, ref cost);

		return cost;
	}

	public static void SetBaseHeavyCost(string key, int value)
	{
		SetBaseHeavyCost(key, Upgrade.None, value);
		SetBaseHeavyCost(key, Upgrade.A, value);
		SetBaseHeavyCost(key, Upgrade.B, value);
	}

	public static void SetBaseSpecialCost(string key, int value)
	{
		SetBaseSpecialCost(key, Upgrade.None, value);
		SetBaseSpecialCost(key, Upgrade.A, value);
		SetBaseSpecialCost(key, Upgrade.B, value);
	}

	public static void SetBaseSpecialCost(string key, Upgrade upgrade, int value)
	{
		ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(BaseSpecialCost, key, out var perUpgradeExists);
		if (!perUpgradeExists)
			perUpgrade = [];
		perUpgrade![upgrade] = value;
	}

	public static void SetBaseHeavyCost(string key, Upgrade upgrade, int value)
	{
		ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(BaseHeavyCost, key, out var perUpgradeExists);
		if (!perUpgradeExists)
			perUpgrade = [];
		perUpgrade![upgrade] = value;
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

		state.ship.Add(SpecialStatus.Status, -GetSpecialCost(state, combat, card));
		state.ship.Add(HeavyStatus.Status, -GetHeavyCost(state, combat, card));
	}
}