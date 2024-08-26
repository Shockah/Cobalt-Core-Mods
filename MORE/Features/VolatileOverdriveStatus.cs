using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.MORE;

internal sealed class VolatileOverdriveStatus : IRegisterable, IStatusLogicHook, IStatusRenderHook
{
	internal static VolatileOverdriveStatus Instance { get; private set; } = null!;
	internal IStatusEntry Entry { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Instance = new();

		Instance.Entry = helper.Content.Statuses.RegisterStatus("VolatileOverdrive", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/VolatileOverdrive.png")).Sprite,
				color = DB.statuses[Status.overdrive].color,
				affectedByTimestop = true,
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "VolatileOverdrive", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "VolatileOverdrive", "description"]).Localize
		});

		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.ModifyBaseDamage), (State state, Combat? combat, bool fromPlayer) =>
		{
			var ship = fromPlayer ? state.ship : (combat ?? DB.fakeCombat).otherShip;
			return ship.Get(Instance.Entry.Status);
		}, 0);

		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerTakeNormalDamage), (State state, Combat combat, Part? part) =>
		{
			if (part is not { type: PType.cockpit })
				return;
			if (state.ship.Get(Instance.Entry.Status) <= 0)
				return;

			combat.QueueImmediate([
				new AStatus
				{
					targetPlayer = true,
					status = Instance.Entry.Status,
					statusAmount = -1
				},
				new AStatus
				{
					targetPlayer = false,
					status = Status.overdrive,
					statusAmount = 1
				}
			]);
		}, 0);

		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnEnemyGetHit), (Combat combat, Part? part) =>
		{
			if (part is not { type: PType.cockpit })
				return;
			if (combat.otherShip.Get(Instance.Entry.Status) <= 0)
				return;

			combat.QueueImmediate([
				new AStatus
				{
					targetPlayer = false,
					status = Instance.Entry.Status,
					statusAmount = -1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.overdrive,
					statusAmount = 1
				}
			]);
		}, 0);

		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(Instance, 0);
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(Instance, 0);
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != Entry.Status)
			return false;
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;

		amount -= Math.Sign(amount);
		return false;
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, bool isForShipStatus, List<Tooltip> tooltips)
		=> status == Entry.Status ? tooltips.Concat(StatusMeta.GetTooltips(Status.overdrive, 1)).ToList() : tooltips;
}