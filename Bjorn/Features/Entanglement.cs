using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System;
using System.Reflection;
using Shockah.Dracula;

namespace Shockah.Bjorn;

internal sealed class EntanglementManager : IRegisterable
{
	internal static IStatusEntry EntanglementStatus { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		EntanglementStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Entanglement", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/Entanglement.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
				affectedByTimestop = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Entanglement", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Entanglement", "description"]).Localize
		});

		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(new StatusLogicHook());

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Postfix))
		);
		
		helper.ModRegistry.AwaitApi<IDraculaApi>(
			"Shockah.Dracula",
			api => api.RegisterBloodTapOptionProvider(EntanglementStatus.Status, (_, _, status) => [
				new AHurt { targetPlayer = true, hurtAmount = 1 },
				new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = 2 },
			])
		);
	}

	private static void AMove_Begin_Postfix(AMove __instance, State s, Combat c)
	{
		if (__instance.dir == 0)
			return;

		var entanglementDepth = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(__instance, "EntanglementDepth");
		if (entanglementDepth >= 2)
			return;

		var ship = __instance.targetPlayer ? s.ship : c.otherShip;
		var entanglement = ship.Get(EntanglementStatus.Status);
		if (entanglement == 0)
			return;

		var action = new AMove { targetPlayer = !__instance.targetPlayer, dir = -__instance.dir * Math.Sign(entanglement), statusPulse = EntanglementStatus.Status };
		ModEntry.Instance.Helper.ModData.CopyAllModData(__instance, action);
		ModEntry.Instance.Helper.ModData.SetModData(action, "EntanglementDepth", entanglementDepth + 1);
		c.QueueImmediate(action);
	}

	private sealed class StatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
		{
			if (args.Status != EntanglementStatus.Status)
				return false;
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
				return false;
			if (args.Amount == 0)
				return false;

			args.Amount -= Math.Sign(args.Amount);
			return false;
		}
	}
}