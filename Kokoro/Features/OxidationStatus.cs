using CobaltCoreModding.Definitions.ExternalItems;
using System.Collections.Generic;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
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
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IOxidationStatusApi OxidationStatus { get; } = new OxidationStatusApi();
		
		public sealed class OxidationStatusApi : IKokoroApi.IV2.IOxidationStatusApi
		{
			public Status Status
				=> Instance.Content.OxidationStatus.Status;
			
			public int GetOxidationStatusMaxValue(State state, Ship ship)
				=> OxidationStatusManager.Instance.GetOxidationStatusMaxValue(state, ship);

			public void RegisterHook(IKokoroApi.IV2.IOxidationStatusApi.IHook hook, double priority = 0)
				=> OxidationStatusManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IOxidationStatusApi.IHook hook)
				=> OxidationStatusManager.Instance.Unregister(hook);
		}
	}
}

internal sealed class OxidationStatusManager : VariedApiVersionHookManager<IKokoroApi.IV2.IOxidationStatusApi.IHook, IOxidationStatusHook>, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	private const int BaseOxidationStatusMaxValue = 7;

	internal static readonly OxidationStatusManager Instance = new();

	private OxidationStatusManager() : base(hook => new V1ToV2OxidationStatusHookWrapper(hook))
	{
	}

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

	public bool HandleStatusTurnAutoStep(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		if (status == Status.corrode && ship.Get(ModEntry.Instance.Content.OxidationStatus.Status) >= GetOxidationStatusMaxValue(state, ship))
		{
			amount++;
			setStrategy = IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct;
			return false;
		}
		if (status == ModEntry.Instance.Content.OxidationStatus.Status && amount >= GetOxidationStatusMaxValue(state, ship))
		{
			amount = 0;
			setStrategy = IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueSet;
			return false;
		}

		return false;
	}
}

internal sealed class V1ToV2OxidationStatusHookWrapper(IOxidationStatusHook v1) : IKokoroApi.IV2.IOxidationStatusApi.IHook
{
	public int ModifyOxidationRequirement(State state, Ship ship, int value)
		=> v1.ModifyOxidationRequirement(state, ship, value);
}