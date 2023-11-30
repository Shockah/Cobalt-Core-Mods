using FSPRO;
using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class IsaacMaxArtifact : DuoArtifact
{
	private static bool WaitingForActionDrain = false;
	private static bool IsDuringTryPlayCard = false;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToDiscard)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_SendCardToDiscard_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToExhaust)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_SendCardToExhaust_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_DrainCardActions_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Finalizer))
		);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		WaitingForActionDrain = false;
	}

	private static void DoAction(State state, Combat combat)
	{
		if (WaitingForActionDrain || !combat.isPlayerTurn)
			return;

		var artifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is IsaacMaxArtifact);
		if (artifact is null)
			return;

		var shipMinX = state.ship.x;
		var shipMaxX = state.ship.x + state.ship.parts.Count - 1;
		var midrowObjects = combat.stuff
			.Where(kvp => kvp.Key >= shipMinX && kvp.Key <= shipMaxX)
			.Select(kvp => kvp.Value)
			.ToList();
		var emptyMidrowSlots = Enumerable.Range(shipMinX, shipMaxX - shipMinX + 1)
			.Where(x => !midrowObjects.Any(o => o.x == x))
			.ToList();

		bool didSomething = false;

		if (midrowObjects.Any(o => !o.bubbleShield))
		{
			var midrowObject = midrowObjects[state.rngActions.NextInt() % midrowObjects.Count];
			combat.QueueImmediate(new ADelegateAction(() =>
			{
				midrowObject.bubbleShield = true;
				Audio.Play(Event.Status_PowerUp);
			}));
			didSomething = true;
		}
		else if (emptyMidrowSlots.Any())
		{
			var slot = emptyMidrowSlots[state.rngActions.NextInt() % emptyMidrowSlots.Count];
			var missilePartX = state.ship.parts.FindIndex(p => p.active && p.type == PType.missiles);
			if (missilePartX != -1)
			{
				var offset = slot - (shipMinX + missilePartX);
				combat.QueueImmediate(new ASpawn
				{
					thing = new Asteroid
					{
						yAnimation = 0.0
					},
					offset = offset
				});
				didSomething = true;
			}
		}

		if (didSomething)
		{
			WaitingForActionDrain = true;
			artifact.Pulse();
		}
	}

	private static void Combat_SendCardToDiscard_Postfix(Combat __instance, State s)
	{
		if (IsDuringTryPlayCard)
			return;
		DoAction(s, __instance);
	}

	private static void Combat_SendCardToExhaust_Postfix(Combat __instance, State s)
		=> DoAction(s, __instance);

	private static void Combat_DrainCardActions_Postfix(Combat __instance)
	{
		if (__instance.cardActions.Count == 0)
			WaitingForActionDrain = false;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}