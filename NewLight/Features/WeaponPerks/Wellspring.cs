using System.Linq;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class WellspringWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Wellspring.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Wellspring", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Wellspring", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Wellspring")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Wellspring", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Wellspring", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = _ => true;
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			combat.Queue(new Action());
		});
	}

	private sealed class Action : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			var abilitiesToCharge = c.Abilities
				.Where(card => Abilities.GetAbilityType(card.Key()) != Abilities.SUPER_ABILITY)
				.Select(card => (Card: card, Cooldown: Abilities.GetCooldown(s, c, card)))
				.Where(e => e.Cooldown > 0)
				.OrderByDescending(e => e.Cooldown)
				.ToList();

			if (abilitiesToCharge.Count == 0)
			{
				timer = 0;
				return;
			}

			var longestCooldown = abilitiesToCharge[0].Cooldown;

			abilitiesToCharge = abilitiesToCharge
				.Where(e => e.Cooldown == longestCooldown)
				.ToList();

			var abilityToCharge = abilitiesToCharge.Count == 1 ? abilitiesToCharge[0] : abilitiesToCharge[s.rngActions.NextInt() % abilitiesToCharge.Count];
			Abilities.SetCurrentCooldown(s, c, abilityToCharge.Card, Abilities.GetCurrentCooldown(s, c, abilityToCharge.Card) - 1);
			Audio.Play(Event.Status_PowerUp);
		}
	}
}