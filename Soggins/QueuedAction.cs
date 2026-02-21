using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

file static class CombatExt
{
	extension(Combat combat)
	{
		public List<QueuedAction> QueuedActions
			=> ModEntry.Instance.Helper.ModData.ObtainModData<List<QueuedAction>>(combat, "QueuedActions");
	}
}

internal abstract class QueuedAction
{
	// public bool WaitForCombatQueueDrain;
	public double? WaitForTotalGameTime;

	public static void Queue(Combat combat, QueuedAction action)
		=> combat.QueuedActions.Add(action);

	public static void Tick(G g, State state, Combat combat)
	{
		var list = combat.QueuedActions;
		foreach (var action in list.ToList())
		{
			if (action.WaitForTotalGameTime is { } waitForTotalGameTime && MG.inst.g.time < waitForTotalGameTime)
				continue;
			// if (action.WaitForCombatQueueDrain && (combat.cardActions.Count != 0 || combat.currentCardAction is not null))
			// 	continue;

			list.Remove(action);
			action.Begin(g, state, combat);
		}
	}

	protected virtual void Begin(G g, State state, Combat combat)
	{
	}
}