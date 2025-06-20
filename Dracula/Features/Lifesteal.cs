using HarmonyLib;
using System;

namespace Shockah.Dracula;

internal static class LifestealExt
{
	public static int GetLifestealMultiplier(this AAttack self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(self, "LifestealMultiplier");

	public static AAttack SetLifestealMultiplier(this AAttack self, int value)
	{
		ModEntry.Instance.Helper.ModData.SetModData(self, "LifestealMultiplier", value);
		return self;
	}

	public static int GetPendingLifesteal(this Ship self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(self, "PendingLifesteal");

	public static void SetPendingLifesteal(this Ship self, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(self, "PendingLifesteal", value);
}

internal sealed class LifestealManager
{
	private static AAttack? AttackContext { get; set; }

	public LifestealManager()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Postfix))
		);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	public sealed class AApplyLifesteal : CardAction
	{
		public required bool TargetPlayer;

		public AApplyLifesteal()
		{
			canRunAfterKill = true;
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var ship = TargetPlayer ? s.ship : c.otherShip;
			var lifesteal = ship.GetPendingLifesteal();

			if (lifesteal <= 0)
			{
				timer = 0;
				return;
			}

			c.QueueImmediate(new AHeal
			{
				targetPlayer = true,
				healAmount = lifesteal,
				canRunAfterKill = true
			});
			ship.SetPendingLifesteal(0);
		}
	}

	private static void Ship_NormalDamage_Prefix(Ship __instance, out (int Shield, int TempShield) __state)
		=> __state = (Shield: __instance.Get(Status.shield), TempShield: __instance.Get(Status.tempShield));

	private static void Ship_NormalDamage_Postfix(Ship __instance, State s, Combat c, DamageDone __result, ref (int Shield, int TempShield) __state)
	{
		if (AttackContext is not { } attack)
			return;

		var lifestealMultiplier = attack.GetLifestealMultiplier();
		if (lifestealMultiplier <= 0)
			return;

		var totalDamage = __result.hullAmt
			+ Math.Max(__state.Shield - __instance.Get(Status.shield), 0)
			+ Math.Max(__state.TempShield - __instance.Get(Status.tempShield), 0);
		if (totalDamage <= 0)
			return;

		var lifestealShip = __instance.isPlayerShip ? c.otherShip : s.ship;
		lifestealShip.SetPendingLifesteal(lifestealShip.GetPendingLifesteal() + totalDamage * lifestealMultiplier);
	}
}