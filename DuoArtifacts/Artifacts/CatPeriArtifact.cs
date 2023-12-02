using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatPeriArtifact : DuoArtifact
{
	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		var data = card.GetDataWithOverrides(state);
		if (!data.temporary)
			return;
		if (!card.GetActions(state, combat).Any(a => a is AAttack))
			return;

		state.ship.Add(Status.overdrive);
		Pulse();
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (state.ship.Get(Status.overdrive) > 0)
		{
			state.ship.Add(Status.overdrive, -1);
			Pulse();
		}
	}
}