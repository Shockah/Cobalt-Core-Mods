using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class OxidationStatusManager : HookManager<IOxidationStatusHook>, IStatusLogicHook, IStatusRenderHook
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

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		var oxidationMaxValue = ship is null ? BaseOxidationStatusMaxValue : GetOxidationStatusMaxValue(MG.inst.g.state ?? DB.fakeState, ship);
		for (int i = 0; i < tooltips.Count; i++)
		{
			var tooltip = tooltips[i];
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{Instance.Content.OxidationStatus.Status}")
				glossary.vals = [$"<c=boldPink>{oxidationMaxValue}</c>"];
		}
		return tooltips;
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;

		if (status == Status.corrode && ship.Get(Instance.Content.OxidationStatus.Status) >= GetOxidationStatusMaxValue(state, ship))
		{
			amount++;
			setStrategy = StatusTurnAutoStepSetStrategy.Direct;
			return false;
		}
		if (status == Instance.Content.OxidationStatus.Status && amount >= GetOxidationStatusMaxValue(state, ship))
		{
			amount = 0;
			setStrategy = StatusTurnAutoStepSetStrategy.QueueSet;
			return false;
		}

		return false;
	}
}
