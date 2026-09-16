using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class ExoticEngramCard : GuardianCard, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Generic", "ExoticEngram", "Name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state) with { description = ModEntry.Instance.Localizations.Localize(["Card", "Generic", "ExoticEngram", "Description", upgrade.ToString()]) };
		return upgrade switch
		{
			Upgrade.B => data with { cost = 0, exhaust = true },
			_ => data with { cost = 0, singleUse = true },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ACardOffering
			{
				limitDeck = ModEntry.Instance.GuardianDeck.Deck,
				rarityOverride = ExoticWeaponCard.RARITY,
				amount = upgrade == Upgrade.A ? 5 : 3,
				makeAllCardsTemporary = upgrade == Upgrade.B,
				canSkip = upgrade != Upgrade.B,
				overrideUpgradeChances = false,
				inCombat = true,
			},
		];
}