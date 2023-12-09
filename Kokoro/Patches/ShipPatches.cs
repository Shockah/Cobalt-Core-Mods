using HarmonyLib;
using Shockah.Shared;
using System.Linq;

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

	private static void HandleMidrowScorching(State state, Combat combat)
	{
		foreach (var @object in combat.stuff.Values)
		{
			if (Instance.Api.GetScorchingStatus(combat, @object) <= 0)
				continue;

			bool isInvincible = @object.Invincible();
			foreach (var someArtifact in state.EnumerateAllArtifacts())
			{
				if (someArtifact.ModifyDroneInvincibility(state, combat, @object) != true)
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

			Instance.Api.SetScorchingStatus(combat, @object, 0);
			combat.QueueImmediate(@object.GetActionsOnDestroyed(state, combat, wasPlayer: true, @object.x));
			@object.DoDestroyedEffect(state, combat);
			combat.stuff.Remove(@object.x);

			foreach (var someArtifact in state.EnumerateAllArtifacts())
				someArtifact.OnPlayerDestroyDrone(state, combat);
		}
	}

	private static void HandleWormStatus(State state, Combat combat)
	{
		int worm = combat.otherShip.Get((Status)Instance.Content.WormStatus.Id!.Value);
		if (worm <= 0)
			return;

		var partXsWithIntent = Enumerable.Range(0, combat.otherShip.parts.Count)
			.Where(x => combat.otherShip.parts[x].intent is not null)
			.Select(x => x + combat.otherShip.x)
			.ToList();

		foreach (var partXWithIntent in partXsWithIntent.Shuffle(state.rngActions).Take(worm))
			combat.Queue(new AStunPart { worldX = partXWithIntent });

		combat.otherShip.Add((Status)Instance.Content.WormStatus.Id!.Value, -1);
	}

	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;

		HandleMidrowScorching(s, c);
		HandleWormStatus(s, c);
	}
}
