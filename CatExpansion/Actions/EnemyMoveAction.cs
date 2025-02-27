using System;
using System.Collections.Generic;
using Nickel;

namespace Shockah.CatExpansion;

internal sealed class EnemyMoveAction : AMove
{
	public EnemyMoveAction()
	{
		targetPlayer = false;
	}
	
	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{GetType().Namespace!}::EnemyMove")
			{
				Icon = dir > 0 ? StableSpr.icons_moveRightEnemy : StableSpr.icons_moveLeftEnemy,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "EnemyMove", dir > 0 ? "right" : "left", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "EnemyMove", dir > 0 ? "right" : "left", "description"], new { Amount = Math.Abs(dir) }),
			}
		];
}