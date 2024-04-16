using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class EphemeralUpgrades : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(EphemeralCannon), nameof(EphemeralCannon.GetActions)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EphemeralCannon_GetActions_Postfix)))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(EphemeralCannon), nameof(EphemeralCannon.GetData)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EphemeralCannon_GetData_Postfix)))
		);
		DB.cardMetas[typeof(EphemeralCannon).Name].upgradesTo = [Upgrade.A, Upgrade.B];

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(EphemeralDodge), nameof(EphemeralDodge.GetActions)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EphemeralDodge_GetActions_Postfix)))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(EphemeralDodge), nameof(EphemeralDodge.GetData)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EphemeralDodge_GetData_Postfix)))
		);
		DB.cardMetas[typeof(EphemeralDodge).Name].upgradesTo = [Upgrade.A, Upgrade.B];

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(EphemeralRepairs), nameof(EphemeralRepairs.GetActions)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EphemeralRepairs_GetActions_Postfix)))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(EphemeralRepairs), nameof(EphemeralRepairs.GetData)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EphemeralRepairs_GetData_Postfix)))
		);
		DB.cardMetas[typeof(EphemeralRepairs).Name].upgradesTo = [Upgrade.A, Upgrade.B];
	}

	private static void EphemeralCannon_GetActions_Postfix(Card __instance, State s, ref List<CardAction> __result)
	{
		__result = __instance.upgrade switch
		{
			Upgrade.A => [
				new RemoveThisCardAction { CardId = __instance.uuid, disabled = __instance.flipped },
				new AAttack { damage = __instance.GetDmg(s, 7), disabled = __instance.flipped },
				new ADummyAction(),
				new ADrawCard { count = 1, disabled = !__instance.flipped }
			],
			Upgrade.B => [
				new AAttack { damage = __instance.GetDmg(s, 7) }
			],
			_ => __result
		};
	}

	private static void EphemeralCannon_GetData_Postfix(Card __instance, ref CardData __result)
	{
		switch (__instance.upgrade)
		{
			case Upgrade.A:
				__result.cost = 0;
				__result.singleUse = false;
				__result.floppable = true;
				__result.art = __instance.flipped ? StableSpr.cards_MiningDrill_Bottom : StableSpr.cards_MiningDrill_Top;
				break;
			case Upgrade.B:
				__result.cost = 2;
				__result.singleUse = false;
				__result.exhaust = true;
				break;
		}
	}

	private static void EphemeralDodge_GetActions_Postfix(Card __instance, ref List<CardAction> __result)
	{
		__result = __instance.upgrade switch
		{
			Upgrade.A => [
				new RemoveThisCardAction { CardId = __instance.uuid, disabled = __instance.flipped },
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 5, disabled = __instance.flipped },
				new ADummyAction(),
				new ADrawCard { count = 1, disabled = !__instance.flipped }
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 5 }
			],
			_ => __result
		};
	}

	private static void EphemeralDodge_GetData_Postfix(Card __instance, ref CardData __result)
	{
		switch (__instance.upgrade)
		{
			case Upgrade.A:
				__result.cost = 0;
				__result.singleUse = false;
				__result.floppable = true;
				__result.art = __instance.flipped ? StableSpr.cards_MiningDrill_Bottom : StableSpr.cards_MiningDrill_Top;
				break;
			case Upgrade.B:
				__result.cost = 2;
				__result.singleUse = false;
				__result.exhaust = true;
				break;
		}
	}

	private static void EphemeralRepairs_GetActions_Postfix(Card __instance, State s, Combat c, ref List<CardAction> __result)
	{
		__result = __instance.upgrade switch
		{
			Upgrade.A => [
				new RemoveThisCardAction { CardId = __instance.uuid, disabled = __instance.flipped },
				new AHeal { targetPlayer = true, healAmount = 4, disabled = __instance.flipped },
				new ADummyAction(),
				new ADrawCard { count = 1, disabled = !__instance.flipped },
				new AEnergy { changeAmount = __instance.GetData(s).cost, disabled = !__instance.flipped }
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.EnergyResource(),
						amount: 4
					),
					action: new AHeal { targetPlayer = true, healAmount = 4 }
				)
			],
			_ => __result
		};
	}

	private static void EphemeralRepairs_GetData_Postfix(Card __instance, ref CardData __result)
	{
		switch (__instance.upgrade)
		{
			case Upgrade.A:
				__result.cost = 1;
				__result.singleUse = false;
				__result.floppable = true;
				__result.art = __instance.flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top;
				break;
			case Upgrade.B:
				__result.cost = 1;
				__result.singleUse = false;
				__result.exhaust = true;
				break;
		}
	}

	private sealed class RemoveThisCardAction : CardAction
	{
		public required int CardId;

		public override Icon? GetIcon(State s)
			=> new Icon { path = StableSpr.icons_singleUse };

		public override List<Tooltip> GetTooltips(State s)
			=> [
				new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
				{
					Icon = StableSpr.icons_singleUse,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["action", "RemoveThisCard", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["action", "RemoveThisCard", "description"])
				}
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			s.RemoveCardFromWhereverItIs(CardId);
		}
	}
}
