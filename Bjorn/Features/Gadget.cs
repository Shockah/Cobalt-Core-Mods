using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Dracula;
using Shockah.Kokoro;

namespace Shockah.Bjorn;

internal sealed class GadgetManager : IRegisterable
{
	private const int MaxStacks = 15;
	
	internal static IStatusEntry GadgetStatus { get; private set; } = null!;
	internal static IStatusEntry TerminationStatus { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		GadgetStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Gadget", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/Gadget.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Gadget", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Gadget", "description"]).Localize
		});
		
		TerminationStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("GadgetTermination", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/GadgetTermination.png")).Sprite,
				color = new("ED2424"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "GadgetTermination", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "GadgetTermination", "description"]).Localize
		});
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			var stacks = state.ship.Get(GadgetStatus.Status);

			switch (stacks)
			{
				case >= MaxStacks:
					state.rewardsQueue.QueueImmediate(new AArtifactOffering
					{
						amount = 2,
						limitPools = [ArtifactPool.Common],
					});
				
					ModEntry.Instance.Helper.ModData.RemoveModData(state, "GadgetProgress");
					break;
				case > 0:
					ModEntry.Instance.Helper.ModData.SetModData(state, "GadgetProgress", stacks);
					break;
				default:
					ModEntry.Instance.Helper.ModData.RemoveModData(state, "GadgetProgress");
					break;
			}
		});

		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnCombatStart), (State state) =>
		{
			state.ship.Set(GetCorrectStatus(state), ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(state, "GadgetProgress"));
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Postfix))
		);
		
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(new TerminationStatusLogicHook());
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(new GadgetAndTerminationStatusRenderingHook());
		
		helper.ModRegistry.AwaitApi<IDraculaApi>(
			"Shockah.Dracula",
			api => api.RegisterBloodTapOptionProvider(GadgetStatus.Status, (_, _, status) => [
				new AHurt { targetPlayer = true, hurtAmount = 1 },
				new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
			])
		);
	}
	
	public static Status GetCorrectStatus(State state)
		=> IsAtLastCombatNode(state) ? TerminationStatus.Status : GadgetStatus.Status;

	private static bool IsAtLastCombatNode(State state)
		=> !state.IsOutsideRun() && state.map.IsFinalZone() && state.map.GetCurrent().contents is MapBattle { battleType: BattleType.Boss };

	private static void State_PopulateRun_Postfix(State __instance)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(__instance, "GadgetProgress");

	private sealed class TerminationStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
		{
			if (args.Status != TerminationStatus.Status)
				return false;
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
				return false;
			if (args.Amount == 0)
				return false;
			
			args.Amount = Math.Max(args.Amount - 1, 0);
			return false;
		}

		public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
		{
			if (args.Status != TerminationStatus.Status)
				return;
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
				return;
			if (args.OldAmount == 0)
				return;
			
			args.Combat.QueueImmediate(new SmartShieldAction { TargetPlayer = args.Ship.isPlayerShip, Amount = 1 });
		}
	}

	private sealed class GadgetAndTerminationStatusRenderingHook : IKokoroApi.IV2.IStatusRenderingApi.IHook
	{
		public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? OverrideStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs args)
		{
			if (args.Status != GadgetStatus.Status && args.Status != TerminationStatus.Status)
				return null;
			
			var colors = new Color[MaxStacks];
			for (var i = 0; i < colors.Length; i++)
				colors[i] = args.Amount > i ? ModEntry.Instance.KokoroApi.StatusRendering.DefaultActiveStatusBarColor : ModEntry.Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor;

			return ModEntry.Instance.KokoroApi.StatusRendering.MakeBarStatusInfoRenderer().SetSegments(colors).SetRows(3);
		}

		public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
			=> args.Status == TerminationStatus.Status ? [
				.. args.Tooltips,
				.. new SmartShieldAction { TargetPlayer = args.Ship?.isPlayerShip ?? true, Amount = 1 }.GetTooltips(DB.fakeState),
			] : args.Tooltips;
	}
}