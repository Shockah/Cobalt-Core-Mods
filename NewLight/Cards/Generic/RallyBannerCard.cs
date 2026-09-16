using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class RallyBannerCard : AbilityCard, IHasCustomCardTraits, IRegisterable
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Generic", "RallyBanner", "Name"]).Localize,
		});
		
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.A, 1);
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state) with { description = ModEntry.Instance.Localizations.Localize(["Card", "Generic", "RallyBanner", "Description"]) };
		return upgrade switch
		{
			Upgrade.B => data with { cost = 3, exhaust = true },
			_ => data with { cost = 2, singleUse = true },
		};
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Fleeting.Trait },
			_ => new HashSet<ICardTraitEntry>(),
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new FinishCooldownsAction(),
			new AStatus { targetPlayer = true, status = Ammo.SpecialStatus.Status, statusAmount = 5 },
			new AStatus { targetPlayer = true, status = Ammo.HeavyStatus.Status, statusAmount = 5 },
		];

	private sealed class FinishCooldownsAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var abilitiesToCharge = c.Abilities
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