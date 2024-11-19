using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal sealed class NitroManager : IDynaHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
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
				color = new("EC592B"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "TempNitro", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "TempNitro", "description"]).Localize
		});
		NitroStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Nitro", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Nitro.png")).Sprite,
				color = new("FBAB32"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Nitro", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Nitro", "description"]).Localize
		});

		ModEntry.Instance.Api.RegisterHook(this, 0);
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);
	}

	public int ModifyBlastwaveDamage(Card? card, State state, bool targetPlayer, int blastwaveIndex)
	{
		var ownerShip = targetPlayer ? ((state.route as Combat) ?? DB.fakeCombat).otherShip : state.ship;
		return ownerShip.Get(NitroStatus.Status) + (blastwaveIndex == 0 ? ownerShip.Get(TempNitroStatus.Status) : 0);
	}

	public void OnBlastwaveTrigger(State state, Combat combat, Ship ship, int worldX, bool hitMidrow)
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

	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> args.Status == TempNitroStatus.Status || args.Status == NitroStatus.Status
			? args.Tooltips.Concat(new BlastwaveManager.BlastwaveAction { Source = new(), Damage = args.Amount, WorldX = 0 }.GetTooltips(DB.fakeState)).ToList()
			: args.Tooltips;
}
