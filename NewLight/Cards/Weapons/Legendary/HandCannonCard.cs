using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class HandCannonCard : LegendaryWeaponCard, IRegisterable
{
	protected override Dictionary<WeaponElement, string> WeaponNames { get; } = new()
	{
		{ WeaponElement.Kinetic, "Fatebringer" },
		{ WeaponElement.Arc, "Nation of Beasts" },
		{ WeaponElement.Solar, "Igneous Hammer" },
		{ WeaponElement.Void, "Bottom Dollar" },
		{ WeaponElement.Stasis, "Eyasluna" },
		{ WeaponElement.Strand, "Better Devils" },
	};
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = Rarity,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Weapon.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Weapon", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, 2) },
		];
}