using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;

namespace Shockah.Dyna;

internal sealed class BastionManager : IDynaHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static IStatusEntry BastionStatus { get; private set; } = null!;

	public BastionManager()
	{
		BastionStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Bastion", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Bastion.png")).Sprite,
				color = new("7E7E7E"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Bastion", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Bastion", "description"]).Localize
		});

		ModEntry.Instance.Api.RegisterHook(this, 0);
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);
	}

	public void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX)
	{
		var ownerShip = ship.isPlayerShip ? combat.otherShip : state.ship;
		var amount = ownerShip.Get(BastionStatus.Status);
		if (amount <= 0)
			return;

		combat.QueueImmediate(new GainShieldOrTempShieldAction
		{
			TargetPlayer = ownerShip.isPlayerShip,
			Amount = amount,
			statusPulse = BastionStatus.Status
		});
	}

	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> args.Status == BastionStatus.Status
			? [
				.. args.Tooltips,
				.. StatusMeta.GetTooltips(Status.tempShield, args.Amount),
				.. StatusMeta.GetTooltips(Status.shield, args.Amount)
			] : args.Tooltips;
}
