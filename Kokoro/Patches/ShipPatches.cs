using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Kokoro;

internal static class ShipPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last)
		);
	}

	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;

		foreach (var @object in c.stuff.Values)
		{
			if (Instance.Api.GetScorchingStatus(c, @object) <= 0)
				continue;

			bool isInvincible = @object.Invincible();
			foreach (var someArtifact in s.EnumerateAllArtifacts())
			{
				if (someArtifact.ModifyDroneInvincibility(s, c, @object) != true)
					continue;
				isInvincible = true;
				someArtifact.Pulse();
			}
			if (isInvincible)
				continue;

			if (@object.bubbleShield)
			{
				@object.bubbleShield = false;
				continue;
			}

			Instance.Api.SetScorchingStatus(c, @object, 0);
			c.QueueImmediate(@object.GetActionsOnDestroyed(s, c, wasPlayer: true, @object.x));
			@object.DoDestroyedEffect(s, c);
			c.stuff.Remove(@object.x);

			foreach (var someArtifact in s.EnumerateAllArtifacts())
				someArtifact.OnPlayerDestroyDrone(s, c);
		}
	}
}
