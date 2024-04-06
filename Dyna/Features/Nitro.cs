using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal sealed class NitroManager : IDynaHook, IStatusRenderHook
{
	internal static IStatusEntry TempNitroStatus { get; private set; } = null!;
	internal static IStatusEntry NitroStatus { get; private set; } = null!;

	public NitroManager()
	{
		TempNitroStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("TempNitro", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/TempNitro.png")).Sprite,
				color = new("FF00FF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "TempNitro", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "TempNitro", "description"]).Localize
		});
		NitroStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Nitro", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Nitro.png")).Sprite,
				color = new("FF00FF")
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Nitro", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Nitro", "description"]).Localize
		});

		ModEntry.Instance.Api.RegisterHook(this, 0);
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);
	}

	public int ModifyBlastwaveDamage(Card? card, State state, bool targetPlayer, int blastwaveIndex)
	{
		var ownerShip = targetPlayer ? ((state.route as Combat) ?? DB.fakeCombat).otherShip : state.ship;
		return ownerShip.Get(NitroStatus.Status) + (blastwaveIndex == 0 ? ownerShip.Get(TempNitroStatus.Status) : 0);
	}

	public void OnBlastwaveTrigger(State state, Combat combat, Ship ship, int worldX)
	{
		var ownerShip = ship.isPlayerShip ? combat.otherShip : state.ship;
		if (ownerShip.Get(TempNitroStatus.Status) <= 0)
			return;

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = ownerShip.isPlayerShip,
			mode = AStatusMode.Set,
			status = TempNitroStatus.Status,
			statusAmount = 0
		});
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
		=> status == TempNitroStatus.Status || status == NitroStatus.Status ? tooltips.Concat(new BlastwaveManager.BlastwaveAction { Source = new(), Damage = amount, WorldX = 0 }.GetTooltips(DB.fakeState)).ToList() : tooltips;
}
