using Nickel;
using System.Collections.Generic;

namespace Shockah.Bloch;

internal sealed class IntuitionManager : IStatusLogicHook, IStatusRenderHook
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

		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
	}

	public void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != IntuitionStatus.Status)
			return;
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return;

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = ship.isPlayerShip,
			status = AuraManager.InsightStatus.Status,
			statusAmount = oldAmount
		});
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		if (status == IntuitionStatus.Status)
			return [..tooltips, ..StatusMeta.GetTooltips(AuraManager.InsightStatus.Status, amount)];
		return tooltips;
	}
}
