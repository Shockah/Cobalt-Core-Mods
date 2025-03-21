using System;
using System.Collections.Generic;
using FSPRO;
using Nickel;

namespace Shockah.Bloch;

internal sealed class DiscardSideManager
{
	internal static ISpriteEntry DiscardLeftIcon { get; private set; } = null!;
	internal static ISpriteEntry DiscardRightIcon { get; private set; } = null!;
	
	public DiscardSideManager()
	{
		DiscardLeftIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/DiscardLeft.png"));
		DiscardRightIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Actions/DiscardRight.png"));
	}
}

internal sealed class DiscardSideAction : CardAction
{
	public required int Amount;
	public required bool Left;

	public override Icon? GetIcon(State s)
		=> new() { path = (Left ? DiscardSideManager.DiscardLeftIcon : DiscardSideManager.DiscardRightIcon).Sprite, number = Amount, color = Colors.textMain };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{GetType().Namespace!}::DiscardSide::{(Left ? "Left" : "Right")}")
			{
				Icon = (Left ? DiscardSideManager.DiscardLeftIcon : DiscardSideManager.DiscardRightIcon).Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "DiscardSide", "name", Left ? "Left" : "Right"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "DiscardSide", "description", Left ? "Left" : "Right"], new { Amount }),
			}
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var amount = Math.Min(Amount, c.hand.Count);
		
		for (var i = 0; i < amount; i++)
		{
			var card = Left ? c.hand[0] : c.hand[^1];
			c.hand.RemoveAt(Left ? 0 : c.hand.Count - 1);
			card.waitBeforeMoving = i * 0.05;
			card.OnDiscard(s, c);
			c.SendCardToDiscard(s, card);
		}
		
		if (amount > 0)
			Audio.Play(Event.CardHandling);
	}
}