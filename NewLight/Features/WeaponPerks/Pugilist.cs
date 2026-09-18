using System.Linq;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class PugilistWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Pugilist.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Pugilist", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Pugilist", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Pugilist")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Pugilist", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Pugilist", "Description"]),
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
				.Where(card => Abilities.GetAbilityType(card.Key()) == Abilities.MELEE_ABILITY)
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