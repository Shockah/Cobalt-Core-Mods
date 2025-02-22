using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Nickel;

namespace Shockah.DuoArtifacts;

internal sealed class DrakeMaxArtifact : DuoArtifact
{
	public bool WaitingForActionDrain;
	
	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToExhaust)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_SendCardToExhaust_Postfix))
		);
	}
	
	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("cardtrait.exhaust"),
			new TTGlossary("action.overheat"),
			new TTGlossary("status.heat", $"<c=boldPink>{(MG.inst.g?.state ?? DB.fakeState).ship.heatTrigger}</c>"),
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		WaitingForActionDrain = false;
	}

	public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
	{
		base.OnQueueEmptyDuringPlayerTurn(state, combat);
		if (!WaitingForActionDrain)
			return;

		WaitingForActionDrain = false;
		if (state.ship.Get(Status.heat) < state.ship.heatTrigger)
			return;
		
		combat.Queue(new AStatus
		{
			targetPlayer = true,
			status = Status.heat,
			mode = AStatusMode.Set,
			statusAmount = state.ship.heatTrigger - 1,
			artifactPulse = Key(),
		});
	}

	private static void Combat_SendCardToExhaust_Postfix(State s)
	{
		if (s.EnumerateAllArtifacts().OfType<DrakeMaxArtifact>().FirstOrDefault() is not { } artifact)
			return;
		artifact.WaitingForActionDrain = true;
	}
}