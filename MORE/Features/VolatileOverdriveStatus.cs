using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.MORE;

internal sealed class VolatileOverdriveStatus : IRegisterable, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
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
		});

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
		});

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
		});

		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(Instance);
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(Instance);
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status != Entry.Status)
			return false;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		args.Amount -= Math.Sign(args.Amount);
		return false;
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> args.Status == Entry.Status ? args.Tooltips.Concat(StatusMeta.GetTooltips(Status.overdrive, 1)).ToList() : args.Tooltips;
}