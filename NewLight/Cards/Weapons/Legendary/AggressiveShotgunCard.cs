using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class AggressiveShotgunCard : LegendaryWeaponCard, WeaponCard.IUsesAmmo.IOnlyOne, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Kinetic, "Astral Horizon" },
		{ WeaponElement.Arc, "Found Verdict" },
		{ WeaponElement.Solar, "Compass Rose" },
		{ WeaponElement.Void, "A Sudden Death" },
		{ WeaponElement.Stasis, "Fractethyst" },
		{ WeaponElement.Strand, "Supercluster" },
	};

	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Weapon.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "AggressiveShotgun"]).Localize,
		});
		
		Ammo.SetBaseSpecialCost(entry.UniqueName, 1);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 2, flippable = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AMove { targetPlayer = true, dir = 1 },
			new ScatterAction { damage = GetDmg(s, 4) },
		];
}