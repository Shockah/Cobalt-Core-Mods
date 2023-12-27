using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

internal sealed class QueuedAction
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly List<QueuedAction> QueuedActions = new();

	public Action? Action { get; set; }
	public bool WaitForCombatQueueDrain { get; set; }
	public double? WaitForTotalGameTime { get; set; }

	public static void Queue(QueuedAction action)
		=> QueuedActions.Add(action);

	public static void Tick(Combat combat)
	{
		foreach (var action in QueuedActions.ToList())
		{
			if (action.WaitForTotalGameTime is { } waitForTotalGameTime && Instance.KokoroApi.TotalGameTime.TotalSeconds < waitForTotalGameTime)
				continue;
			if (action.WaitForCombatQueueDrain && (combat.cardActions.Count != 0 || combat.currentCardAction is not null))
				continue;

			QueuedActions.Remove(action);
			action.Action?.Invoke();
		}
	}
}