using Microsoft.Xna.Framework.Graphics;
using Shockah.Shared;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class MidrowScorchingManager : HookManager<IMidrowScorchingHook>
{
	private static ModEntry Instance => ModEntry.Instance;

	internal void OnPlayerTurnStart(State state, Combat combat)
	{
		foreach (var @object in combat.stuff.Values)
		{
			if (Instance.Api.GetScorchingStatus(state, combat, @object) <= 0)
				continue;

			bool isInvincible = @object.Invincible();
			foreach (var someArtifact in state.EnumerateAllArtifacts())
			{
				if (someArtifact.ModifyDroneInvincibility(state, combat, @object) != true)
					continue;
				isInvincible = true;
				someArtifact.Pulse();
			}
			if (isInvincible)
				continue;

			if (@object.bubbleShield)
			{
				@object.bubbleShield = false;
				continue;
			}

			Instance.Api.SetScorchingStatus(state, combat, @object, 0);
			combat.QueueImmediate(@object.GetActionsOnDestroyed(state, combat, wasPlayer: true, @object.x));
			@object.DoDestroyedEffect(state, combat);
			combat.stuff.Remove(@object.x);

			foreach (var someArtifact in state.EnumerateAllArtifacts())
				someArtifact.OnPlayerDestroyDrone(state, combat);
		}
	}

	internal void ModifyMidrowObjectDestroyedActions(State state, Combat combat, StuffBase @object, bool wasPlayer, List<CardAction> actions)
	{
		var scorching = Instance.Api.GetScorchingStatus(state, combat, @object);
		if (scorching <= 0)
			return;

		actions.Add(new AStatus
		{
			status = Status.heat,
			statusAmount = scorching,
			targetPlayer = wasPlayer
		});
	}

	internal void ModifyMidrowObjectTooltips(StuffBase @object, List<Tooltip> tooltips)
	{
		if (StateExt.Instance is not { } state)
			return;
		if (StateExt.Instance?.route is not Combat combat)
			return;

		var scorching = Instance.Api.GetScorchingStatus(state, combat, @object);
		if (scorching <= 0)
			return;

		tooltips.Add(Instance.Api.GetScorchingTooltip(scorching));
	}

	internal void OnDrawWithHilight(StuffBase @object, G g, Spr sprite, Vec v, bool flipX, bool flipY)
	{
		if (g.state.route is not Combat combat)
			return;
		if (Instance.Api.GetScorchingStatus(g.state, combat, @object) <= 0)
			return;

		var color = new Color(1, 0.35, 0).fadeAlpha(Math.Sin(Instance.TotalGameTime.TotalSeconds * Math.PI * 2) * 0.5 + 0.5);
		Draw.Sprite(sprite, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color);
		Draw.Sprite(sprite, v.x - 1.0, v.y - 1.0, flipX, flipY, color: color, blend: BlendState.Additive);
	}
}
