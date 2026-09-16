using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class HealingRiftCard : GuardianCard, IRegisterable
{
	public static IStatusEntry Status { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "HealingRift", "Name"]).Localize,
		});
		
		Status = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("HealingRift", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/HealingRift.png")).Sprite,
				color = new("7AF48B"),
				isGood = true,
				affectedByTimestop = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "HealingRift", "Status", "Name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "HealingRift", "Status", "Description"]).Localize,
		});
		
		Abilities.SetBaseCooldown(entry.UniqueName, 5);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Postfix))
		);
		
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(new StatusLogicHook());
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 0, exhaust = true },
			_ => base.GetData(state) with { cost = 2 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.Status, statusAmount = 5 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.Status, statusAmount = 3 },
			],
		};
	
	private static void AMove_Begin_Prefix(AMove __instance, State s, Combat c, out int __state)
	{
		var ship = __instance.targetPlayer ? s.ship : c.otherShip;
		__state = ship.x;
	}

	private static void AMove_Begin_Postfix(AMove __instance, State s, Combat c, in int __state)
	{
		var ship = __instance.targetPlayer ? s.ship : c.otherShip;
		if (ship.x == __state)
			return;
		if (ship.Get(Status.Status) == 0)
			return;

		ship.Set(Status.Status, 0);
	}

	private sealed class StatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs args)
			=> new HashSet<Status> { Status.Status };
		
		public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
		{
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
				return false;
			if (args.Amount == 0)
				return false;

			args.Amount = Math.Max(args.Amount - 1, 0);
			return false;
		}
		
		public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
		{
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
				return;
			if (args.OldAmount <= 0)
				return;
		
			args.Combat.QueueImmediate(new AHeal { targetPlayer = args.Ship.isPlayerShip, healAmount = 1 });
		}
	}
}