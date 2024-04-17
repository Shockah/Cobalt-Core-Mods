using Nanoray.PluginManager;
using Nickel;
using System;

namespace Shockah.MORE;

internal sealed class ActionReactionStatus : IRegisterable, IStatusLogicHook
{
	internal static ActionReactionStatus Instance { get; private set; } = null!;
	internal IStatusEntry Entry { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Instance = new();

		Instance.Entry = helper.Content.Statuses.RegisterStatus("ActionReaction", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/ActionReaction.png")).Sprite,
				color = new("FFC646"),
				affectedByTimestop = true,
				isGood = false,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "ActionReaction", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "ActionReaction", "description"]).Localize
		});

		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (State state, Combat combat) =>
		{
			if (state.ship.Get(Instance.Entry.Status) <= 0)
				return;

			combat.Queue(new AHurt
			{
				targetPlayer = true,
				hurtShieldsFirst = true,
				hurtAmount = 1,
				statusPulse = Instance.Entry.Status
			});
		}, 0);

		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(Instance, 0);
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
}