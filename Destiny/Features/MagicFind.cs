using System;
using System.Collections.Generic;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Destiny;

internal sealed class MagicFindManager : IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static IStatusEntry MagicFindStatus { get; private set; } = null!;
	
	public MagicFindManager()
	{
		MagicFindStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("MagicFind", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/MagicFind.png")).Sprite,
				color = new Color("FF6FEC"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "MagicFind", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "MagicFind", "description"]).Localize
		});

		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(this);
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status != MagicFindStatus.Status)
			return false;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;
		if (args.Amount == 0)
			return false;

		args.Amount = Math.Max(args.Amount - 1, 0);
		return false;
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Status != MagicFindStatus.Status)
			return;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return;
		if (args.OldAmount == 0)
			return;

		args.Combat.QueueImmediate(new AStatus
		{
			targetPlayer = args.Ship.isPlayerShip,
			status = Status.shard,
			statusAmount = 1,
		});
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> [
			.. args.Tooltips,
			.. StatusMeta.GetTooltips(Status.shard, 1),
		];
}