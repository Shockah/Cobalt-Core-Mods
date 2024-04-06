using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal sealed class BastionManager : IDynaHook, IStatusRenderHook
{
	internal static IStatusEntry BastionStatus { get; private set; } = null!;

	public BastionManager()
	{
		BastionStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Bastion", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Bastion.png")).Sprite,
				color = new("FF00FF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Bastion", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Bastion", "description"]).Localize
		});

		ModEntry.Instance.Api.RegisterHook(this, 0);
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
	}

	public void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX)
	{
		var ownerShip = ship.isPlayerShip ? combat.otherShip : state.ship;
		var amount = ownerShip.Get(BastionStatus.Status);
		if (amount <= 0)
			return;

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = ownerShip.isPlayerShip,
			status = Status.tempShield,
			statusAmount = amount,
			statusPulse = BastionStatus.Status
		});
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
		=> status == BastionStatus.Status ? tooltips.Concat(StatusMeta.GetTooltips(Status.tempShield, amount)).ToList() : tooltips;
}
