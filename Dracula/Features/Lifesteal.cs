using HarmonyLib;
using Shockah.Shared;
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
}

internal sealed class LifestealManager
{
	private static AAttack? AttackContext { get; set; }
	private static int TotalLifesteal = 0;

	public LifestealManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Postfix))
		);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer(AAttack __instance)
		=> AttackContext = null;

	public sealed class AApplyLifesteal : CardAction
	{
		public AApplyLifesteal()
		{
			canRunAfterKill = true;
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			if (TotalLifesteal <= 0)
			{
				timer = 0;
				return;
			}

			c.QueueImmediate(new AHeal
			{
				targetPlayer = true,
				healAmount = TotalLifesteal,
				canRunAfterKill = true
			});
			TotalLifesteal = 0;
		}
	}

	private static void Ship_NormalDamage_Prefix(Ship __instance, ref (int Shield, int TempShield) __state)
		=> __state = (Shield: __instance.Get(Status.shield), TempShield: __instance.Get(Status.tempShield));

	private static void Ship_NormalDamage_Postfix(Ship __instance, DamageDone __result, ref (int Shield, int TempShield) __state)
	{
		if (AttackContext is not { } attack)
			return;

		int lifestealMultiplier = attack.GetLifestealMultiplier();
		if (lifestealMultiplier <= 0)
			return;

		var totalDamage = __result.hullAmt
			+ Math.Max(__state.Shield - __instance.Get(Status.shield), 0)
			+ Math.Max(__state.TempShield - __instance.Get(Status.tempShield), 0);
		if (totalDamage <= 0)
			return;

		TotalLifesteal += totalDamage * lifestealMultiplier;
	}
}