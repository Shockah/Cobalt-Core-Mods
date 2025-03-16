using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FMOD;
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
				color = new("BE4611"),
				isGood = true,
				affectedByTimestop = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "status", "BarrelSpin", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "status", "BarrelSpin", "description"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(new StatusLogicHook());
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Postfix_Last)), priority: Priority.Last)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StatusMeta), nameof(StatusMeta.GetSound)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StatusMeta_GetSound_Postfix))
		);
	}

	private static void AAttack_Begin_Prefix(Combat c, out List<CardAction> __state)
		=> __state = c.cardActions.ToList();

	private static void AAttack_Begin_Postfix_Last(AAttack __instance, Combat c, in List<CardAction> __state)
	{
		var spin = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(__instance, "BarrelSpin");
		if (spin <= 0)
			return;
		
		__instance.timer /= spin + 1;

		foreach (var action in c.cardActions)
		{
			if (action == __instance)
				continue;
			if (__state.Contains(action))
				continue;
			if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(action, "BarrelSpin") > 0)
				continue;
			
			ModEntry.Instance.Helper.ModData.SetModData(action, "BarrelSpin", spin);
			action.timer /= spin + 1;
		}
	}

	private static void Combat_DrainCardActions_Prefix(Combat __instance, G g)
	{
		for (var i = 0; i < __instance.cardActions.Count; i++)
		{
			if (__instance.cardActions[i] is not AAttack attack)
				continue;
			if (attack.fromDroneX is not null)
				continue;
			
			var sourceShip = attack.targetPlayer ? __instance.otherShip : g.state.ship;
			var totalSpin = sourceShip.Get(BarrelSpinStatus.Status);
			if (totalSpin <= 0)
				return;
		
			var spin = Math.Max(Math.Min(totalSpin, attack.damage - 1), 0);
			if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(attack, "BarrelSpin") > 0)
				return;

			var rotorGreaseArtifact = attack.targetPlayer ? null : g.state.EnumerateAllArtifacts().OfType<BreadnaughtRotorGreaseArtifact>().FirstOrDefault();
			var extraAttacks = spin + (spin < totalSpin && rotorGreaseArtifact is not null ? 1 : 0);
			var hasFirstZeroDamageAttack = extraAttacks > spin;
			if (extraAttacks <= 0)
				return;
			
			attack.damage -= spin;
			ModEntry.Instance.Helper.ModData.SetModData(attack, "BarrelSpin", extraAttacks);
			
			__instance.cardActions.InsertRange(
				i, Enumerable.Range(0, extraAttacks)
					.Select(i =>
					{
						var splitAttack = Mutil.DeepCopy(attack);
						splitAttack.damage = 1;
						if (i == 0 && hasFirstZeroDamageAttack && rotorGreaseArtifact is not null)
						{
							splitAttack.damage = 0;
							if (string.IsNullOrEmpty(splitAttack.artifactPulse))
								splitAttack.artifactPulse = rotorGreaseArtifact.Key();
							else
								rotorGreaseArtifact.Pulse();
						}
						return splitAttack;
					})
			);
			i += spin;
		}
	}
	
	// TODO: replace with a Nickel feature
	private static void StatusMeta_GetSound_Postfix(Status status, ref GUID __result)
	{
		if (status == BarrelSpinStatus.Status)
			__result = default;
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

		public bool? IsAffectedByBoost(IKokoroApi.IV2.IStatusLogicApi.IHook.IIsAffectedByBoostArgs args)
			=> args.Status == BarrelSpinStatus.Status ? false : null;
	}
}