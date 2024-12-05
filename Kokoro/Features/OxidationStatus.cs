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
			
			public int GetOxidationStatusThreshold(State state, Ship ship)
				=> OxidationStatusManager.Instance.GetOxidationStatusMaxValue(state, ship);

			public void RegisterHook(IKokoroApi.IV2.IOxidationStatusApi.IHook hook, double priority = 0)
				=> OxidationStatusManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IOxidationStatusApi.IHook hook)
				=> OxidationStatusManager.Instance.Unregister(hook);
			
			internal sealed class ModifyOxidationRequirementArgs : IKokoroApi.IV2.IOxidationStatusApi.IHook.IModifyOxidationRequirementArgs
			{
				public State State { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public int Threshold { get; internal set; }
			}
		}
	}
}

internal sealed class OxidationStatusManager : VariedApiVersionHookManager<IKokoroApi.IV2.IOxidationStatusApi.IHook, IOxidationStatusHook>, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	private const int BaseOxidationStatusThreshold = 7;

	internal static readonly OxidationStatusManager Instance = new();

	private OxidationStatusManager() : base(ModEntry.Instance.Package.Manifest.UniqueName, new HookMapper<IKokoroApi.IV2.IOxidationStatusApi.IHook, IOxidationStatusHook>(hook => new V1ToV2OxidationStatusHookWrapper(hook)))
	{
	}

	public int GetOxidationStatusMaxValue(State state, Ship ship)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.OxidationStatusApi.ModifyOxidationRequirementArgs>();
		try
		{
			args.State = state;
			args.Ship = ship;
			args.Threshold = BaseOxidationStatusThreshold;

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				args.Threshold += hook.ModifyOxidationRequirement(args);
			return args.Threshold;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		var oxidationMaxValue = args.Ship is null ? BaseOxidationStatusThreshold : GetOxidationStatusMaxValue(MG.inst.g.state ?? DB.fakeState, args.Ship);
		foreach (var tooltip in args.Tooltips)
		{
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{ModEntry.Instance.Content.OxidationStatus.Status}")
				glossary.vals = [$"<c=boldPink>{oxidationMaxValue}</c>"];
		}
		return args.Tooltips;
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		if (args.Status == Status.corrode && args.Ship.Get(ModEntry.Instance.Content.OxidationStatus.Status) >= GetOxidationStatusMaxValue(args.State, args.Ship))
		{
			args.Amount++;
			args.SetStrategy = IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct;
			return false;
		}
		if (args.Status == ModEntry.Instance.Content.OxidationStatus.Status && args.Amount >= GetOxidationStatusMaxValue(args.State, args.Ship))
		{
			args.Amount = 0;
			args.SetStrategy = IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueSet;
			return false;
		}

		return false;
	}
}

internal sealed class V1ToV2OxidationStatusHookWrapper(IOxidationStatusHook v1) : IKokoroApi.IV2.IOxidationStatusApi.IHook
{
	public int ModifyOxidationRequirement(IKokoroApi.IV2.IOxidationStatusApi.IHook.IModifyOxidationRequirementArgs args)
		=> v1.ModifyOxidationRequirement(args.State, args.Ship, args.Threshold);
}