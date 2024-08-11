using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Reflection;

namespace Shockah.Bjorn;

internal sealed class Entanglement : IRegisterable, IStatusRenderHook
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

		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(new StatusLogicHook(), 0);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Postfix))
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

	private sealed class StatusLogicHook : IStatusLogicHook
	{
		public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
		{
			if (status != EntanglementStatus.Status)
				return false;
			if (timing != StatusTurnTriggerTiming.TurnStart)
				return false;
			if (amount == 0)
				return false;

			amount -= Math.Sign(amount);
			return false;
		}
	}
}