using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class PulseRifleCard : LegendaryWeaponCard, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Kinetic, "The Messenger" },
		{ WeaponElement.Arc, "Horror's Least" },
		{ WeaponElement.Solar, "Darkest Before" },
		{ WeaponElement.Void, "Gridskipper" },
		{ WeaponElement.Stasis, "Syncopation -53" },
		{ WeaponElement.Strand, "Relentless" },
	};

	protected override List<string> AllowedPerkUniqueNames { get; } = [
		.. GlobalAllowedPerkUniqueNames,
		ModEntry.Instance.Helper.Content.Cards.RetainCardTrait.UniqueName,
		ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait.UniqueName,
		ModEntry.Instance.Helper.Content.Cards.BuoyantCardTrait.UniqueName,
	];
	
	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Weapon.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Weapon", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 2 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, 1) },
			new AAttack { damage = GetDmg(s, 2) },
		];
}