using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;

namespace Shockah.Bloch;

internal sealed class IntuitionManager : IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static IStatusEntry IntuitionStatus { get; private set; } = null!;

	public IntuitionManager()
	{
		IntuitionStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Intuition", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Intuition.png")).Sprite,
				color = new Color("FF6FEC"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Intuition", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Intuition", "description"]).Localize
		});

		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(this);
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Status != IntuitionStatus.Status)
			return;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return;

		args.Combat.QueueImmediate(new AStatus
		{
			targetPlayer = args.Ship.isPlayerShip,
			status = AuraManager.InsightStatus.Status,
			statusAmount = args.OldAmount
		});
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status == IntuitionStatus.Status)
			return [.. args.Tooltips, .. StatusMeta.GetTooltips(AuraManager.InsightStatus.Status, args.Amount)];
		return args.Tooltips;
	}
}
