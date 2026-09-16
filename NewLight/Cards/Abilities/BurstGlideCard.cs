using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class BurstGlideCard : GuardianCard, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "BurstGlide", "Name"]).Localize,
		});
		
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.None, 2);
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.A, 2);
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.B, 1);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1, flippable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove { targetPlayer = true, dir = 2 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
				new AMove { targetPlayer = true, dir = 2 },
			],
			_ => [
				new AMove { targetPlayer = true, dir = 3 },
			],
		};
}