using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DominateCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Dominate", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Dominate", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			floppable = upgrade != Upgrade.None
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];

		for (var i = 0; i < s.ship.parts.Count; i++)
			if (s.ship.parts[i].type == PType.missiles)
				actions.Add(new APositionalDroneFlip
				{
					WorldX = s.ship.x + i,
					disabled = upgrade != Upgrade.None && flipped
				});

		if (upgrade != Upgrade.None)
			actions.Add(new ADummyAction());

		if (upgrade == Upgrade.A)
		{
			for (var i = 0; i < s.ship.parts.Count; i++)
				if (s.ship.parts[i].type == PType.missiles)
					actions.Add(new APositionalDroneBubble
					{
						WorldX = s.ship.x + i
					});
		}
		else if (upgrade == Upgrade.B)
		{
			for (var i = 0; i < s.ship.parts.Count; i++)
				if (s.ship.parts[i].type == PType.missiles)
					actions.Add(new APositionalDroneTrigger
					{
						WorldX = s.ship.x + i
					});
		}

		actions.Add(new ADrawCard
		{
			count = 1
		});

		return actions;
	}
}
