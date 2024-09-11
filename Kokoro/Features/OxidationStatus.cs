using CobaltCoreModding.Definitions.ExternalItems;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public ExternalStatus OxidationStatus
		=> ExternalStatus.GetRaw((int)Instance.Content.OxidationStatus.Status);

	public Status OxidationVanillaStatus
		=> (Status)OxidationStatus.Id!.Value;

	public Tooltip GetOxidationStatusTooltip(State state, Ship ship)
		=> new TTGlossary($"status.{Instance.Content.OxidationStatus.Status}", OxidationStatusManager.Instance.GetOxidationStatusMaxValue(state, ship));

	public int GetOxidationStatusMaxValue(State state, Ship ship)
		=> OxidationStatusManager.Instance.GetOxidationStatusMaxValue(state, ship);

	public void RegisterOxidationStatusHook(IOxidationStatusHook hook, double priority)
		=> OxidationStatusManager.Instance.Register(hook, priority);

	public void UnregisterOxidationStatusHook(IOxidationStatusHook hook)
		=> OxidationStatusManager.Instance.Unregister(hook);
}

internal sealed class OxidationStatusManager : HookManager<IOxidationStatusHook>, IStatusLogicHook, IStatusRenderHook
{
	private const int BaseOxidationStatusMaxValue = 7;

	internal static readonly OxidationStatusManager Instance = new();

	public int GetOxidationStatusMaxValue(State state, Ship ship)
	{
		var value = BaseOxidationStatusMaxValue;
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			value = hook.ModifyOxidationRequirement(state, ship, value);
		return value;
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		var oxidationMaxValue = ship is null ? BaseOxidationStatusMaxValue : GetOxidationStatusMaxValue(MG.inst.g.state ?? DB.fakeState, ship);
		foreach (var tooltip in tooltips)
		{
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{ModEntry.Instance.Content.OxidationStatus.Status}")
				glossary.vals = [$"<c=boldPink>{oxidationMaxValue}</c>"];
		}
		return tooltips;
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;

		if (status == Status.corrode && ship.Get(ModEntry.Instance.Content.OxidationStatus.Status) >= GetOxidationStatusMaxValue(state, ship))
		{
			amount++;
			setStrategy = StatusTurnAutoStepSetStrategy.Direct;
			return false;
		}
		if (status == ModEntry.Instance.Content.OxidationStatus.Status && amount >= GetOxidationStatusMaxValue(state, ship))
		{
			amount = 0;
			setStrategy = StatusTurnAutoStepSetStrategy.QueueSet;
			return false;
		}

		return false;
	}
}
