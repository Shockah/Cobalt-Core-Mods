﻿using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class OxidationStatusManager : HookManager<IOxidationStatusHook>, IStatusLogicHook
{
	private const int BaseOxidationStatusMaxValue = 7;

	private static ModEntry Instance => ModEntry.Instance;

	public int GetOxidationStatusMaxValue(State state, Ship ship)
	{
		int value = BaseOxidationStatusMaxValue;
		foreach (var hook in GetHooksWithProxies(Instance.Api, state.EnumerateAllArtifacts()))
			value = hook.ModifyOxidationRequirement(state, ship, value);
		return value;
	}

	internal void ModifyStatusTooltips(Ship ship, G g)
	{
		for (int i = 0; i < g.tooltips.tooltips.Count; i++)
		{
			var tooltip = g.tooltips.tooltips[i];
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{Instance.Content.OxidationStatus.Id!.Value}")
				glossary.vals = new object[] { $"<c=boldPink>{GetOxidationStatusMaxValue(g.state, ship)}</c>" };
		}
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;

		if (status == Status.corrode && ship.Get((Status)Instance.Content.OxidationStatus.Id!.Value) >= GetOxidationStatusMaxValue(state, ship))
		{
			amount++;
			setStrategy = StatusTurnAutoStepSetStrategy.Direct;
			return false;
		}
		if (status == (Status)Instance.Content.OxidationStatus.Id!.Value && amount >= GetOxidationStatusMaxValue(state, ship))
		{
			amount = 0;
			setStrategy = StatusTurnAutoStepSetStrategy.QueueSet;
			return false;
		}

		return false;
	}
}
