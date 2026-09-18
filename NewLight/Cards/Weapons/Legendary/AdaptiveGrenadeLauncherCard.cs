using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class AdaptiveGrenadeLauncherCard : LegendaryWeaponCard, WeaponCard.IRepeatable, WeaponCard.IUsesAmmo, WeaponCard.ICannotCrit, IRegisterable
{
	protected override Dictionary<WeaponElement, string> ElementWeaponNames { get; } = new()
	{
		{ WeaponElement.Arc, "Wendigo GL3" },
		{ WeaponElement.Solar, "Canis Major" },
		{ WeaponElement.Void, "Edge Transit" },
		{ WeaponElement.Stasis, "VS Chill Inhibitor" },
		{ WeaponElement.Strand, "The Ever-Present" },
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Legendary", "AdaptiveGrenadeLauncher"]).Localize,
		});
		
		Ammo.SetBaseHeavyCost(entry.UniqueName, 2);
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 2);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with { cost = 1, flippable = true };

	public override IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
	{
		var baseResults = base.GetInnateTraits(state);
		var results = (baseResults as HashSet<ICardTraitEntry>) ?? new HashSet<ICardTraitEntry>(baseResults);
		results.Add(ModEntry.Instance.KokoroApi.Finite.Trait);
		return results;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AMove { targetPlayer = true, dir = 2 },
			new AAttack { damage = GetDmg(s, 4), BlastRange = 1 },
		];
}