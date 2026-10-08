using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class CombinationBlowCard : GuardianCard, IHasCustomCardTraits, IRegisterable
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Ability", "CombinationBlow", "Name"]).Localize,
		});
		
		Abilities.SetAbilityType(entry.UniqueName, Abilities.MELEE_ABILITY);
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.None, 5);
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.A, 5);
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.B, 2);
	}

	public override CardData GetData(State state)
		=> base.GetData(state) with
		{
			cost = 1,
			description = ModEntry.Instance.Localizations.Localize(["Card", "Ability", "CombinationBlow", "Description"], new { Damage = GetDmg(state, BaseDamage) }),
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.A => new HashSet<ICardTraitEntry> { RampageWeaponPerk.Trait },
			_ => new HashSet<ICardTraitEntry>(),
		};

	private int BaseDamage
		=> upgrade switch
		{
			Upgrade.B => 1,
			_ => 2,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, BaseDamage) },
			new Action(),
		];
	
	private sealed class Action : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			
			var abilitiesToCharge = c.Abilities
				.Where(card => Abilities.GetAbilityType(card.Key()) == Abilities.UTILITY_ABILITY)
				.Where(card => Abilities.GetCooldown(s, c, card) > 0)
				.ToList();

			if (abilitiesToCharge.Count == 0)
			{
				timer = 0;
				return;
			}

			foreach (var card in abilitiesToCharge)
				Abilities.SetCurrentCooldown(s, c, card, 0);
			Audio.Play(Event.Status_PowerUp);
		}
	}
}