using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class AdaptiveSidearmCard : LegendaryWeaponCard, IRegisterable
{
	protected override Dictionary<WeaponElement, string> WeaponNames { get; } = new()
	{
		{ WeaponElement.Kinetic, "Spoiler Alert" },
		{ WeaponElement.Arc, "Anonymous Autumn" },
		{ WeaponElement.Solar, "Drang" },
		{ WeaponElement.Void, "Seventh Seraph SI-2" },
		{ WeaponElement.Stasis, "Faustus Decline" },
		{ WeaponElement.Strand, "Mykel's Reverence" },
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
		=> new() { cost = 0 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, 1) },
		];
}