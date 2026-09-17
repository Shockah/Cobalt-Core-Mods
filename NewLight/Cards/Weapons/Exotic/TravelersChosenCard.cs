using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class TravelersChosenCard : ExoticWeaponCard, IHasCustomCardTraits, IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	protected override WeaponElement WeaponElement => WeaponElement.Kinetic;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "TravelersChosen", "Name"]).Localize,
		});
		
		var traitIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/ExoticWeaponPerks/TravelersChosen.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("TravelersChosen", new()
		{
			Icon = (_, _) => traitIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "TravelersChosen", "CardTrait", "Name"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::TravelersChosen")
				{
					Icon = traitIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["Card", "Weapon", "Exotic", "TravelersChosen", "CardTrait", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["Card", "Weapon", "Exotic", "TravelersChosen", "CardTrait", "Description"]),
				}
			]
		});
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, Upgrade.B, 2);
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			combat.QueueImmediate(new BumpCooldownsAction());
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 1 },
			Upgrade.A => base.GetData(state) with { cost = 0, recycle = true },
			_ => base.GetData(state) with { cost = 0 },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { Trait, FullAutoCardTrait.Trait, ModEntry.Instance.KokoroApi.Finite.Trait },
			_ => new HashSet<ICardTraitEntry> { Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack { damage = GetDmg(s, 1) },
		];

	private sealed class BumpCooldownsAction : CardAction
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
				Abilities.SetCurrentCooldown(s, c, card, Abilities.GetCurrentCooldown(s, c, card) - 1);
			Audio.Play(Event.Status_PowerUp);
		}
	}
}