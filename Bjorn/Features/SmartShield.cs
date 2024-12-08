using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.Bjorn;

internal sealed class SmartShieldManager : IRegisterable
{
	internal static ISpriteEntry Icon { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/SmartShield.png"));
	}
}

public sealed class SmartShieldAction : CardAction
{
	public bool TargetPlayer = true;
	public required int Amount;

	public override Icon? GetIcon(State s)
		=> new() { path = SmartShieldManager.Icon.Sprite, number = Amount, color = Colors.textMain };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::SmartShield")
			{
				Icon = SmartShieldManager.Icon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "SmartShield", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "SmartShield", "description"], new { Amount = Amount }),
			}
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		var ship = TargetPlayer ? s.ship : c.otherShip;
		var shieldThatFits = ship.GetMaxShield() - ship.Get(Status.shield);

		var shieldToAdd = Math.Min(shieldThatFits, Amount);
		var tempShieldToAdd = Amount - shieldToAdd;

		List<CardAction> actions = [];

		if (shieldToAdd > 0)
			actions.Add(new AStatus { targetPlayer = TargetPlayer, status = Status.shield, statusAmount = shieldToAdd });
		if (tempShieldToAdd > 0)
			actions.Add(new AStatus { targetPlayer = TargetPlayer, status = Status.tempShield, statusAmount = tempShieldToAdd });

		c.QueueImmediate(actions);
	}
}