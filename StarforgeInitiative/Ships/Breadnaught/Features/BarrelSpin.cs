using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.StarforgeInitiative;

internal sealed class BarrelSpinManager : IRegisterable
{
	internal static IStatusEntry BarrelSpinStatus { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		BarrelSpinStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("BarrelSpin", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Breadnaught/Status/BarrelSpin.png")).Sprite,
				color = new("E1FFCF"),
				isGood = true,
				affectedByTimestop = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "status", "BarrelSpin", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "status", "BarrelSpin", "description"]).Localize
		});
		
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(new StatusLogicHook());
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Prefix))
		);
	}

	private static void Combat_DrainCardActions_Prefix(Combat __instance, G g)
	{
		for (var i = 0; i < __instance.cardActions.Count; i++)
		{
			if (__instance.cardActions[i] is not AAttack attack)
				continue;
			
			var sourceShip = attack.targetPlayer ? __instance.otherShip : g.state.ship;
			var totalSpin = sourceShip.Get(BarrelSpinStatus.Status);
			if (totalSpin <= 0)
				return;
		
			var spin = Math.Max(Math.Min(totalSpin, attack.damage - 1), 0);
			if (spin <= 0)
				return;
			if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(attack, "AffectedByBarrelSpin"))
				return;

			attack.damage -= spin;
			attack.timer /= spin + 1;
			ModEntry.Instance.Helper.ModData.SetModData(attack, "AffectedByBarrelSpin", true);
			
			__instance.cardActions.InsertRange(
				i, Enumerable.Range(0, spin)
					.Select(_ =>
					{
						var splitAttack = Mutil.DeepCopy(attack);
						splitAttack.damage = 1;
						return splitAttack;
					})
			);
			i += spin;
		}
	}

	private sealed class StatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
		{
			if (args.Status != BarrelSpinStatus.Status)
				return false;
			if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
				return false;

			args.Amount = 0;
			return false;
		}
	}
}