using System.Linq;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class ShootToLootWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/ShootToLoot.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("ShootToLoot", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "ShootToLoot", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::ShootToLoot")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ShootToLoot", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ShootToLoot", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.IUsesAmmo;
		
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
			timer = 0;

			var cards = c.hand
				.Where(card => Ammo.GetSpecialCost(s, c, card) > 0 || Ammo.GetHeavyCost(s, c, card) > 0)
				.ToList();
			
			if (cards.Count == 0)
				cards = c.discard.Concat(s.deck)
					.Where(card => Ammo.GetSpecialCost(s, c, card) > 0 || Ammo.GetHeavyCost(s, c, card) > 0)
					.ToList();

			if (cards.Count == 0)
				return;

			var card = cards[s.rngActions.NextInt() % cards.Count];
			c.QueueImmediate(new AStatus { targetPlayer = true, status = Ammo.GetSpecialCost(s, c, card) > 0 ? Ammo.SpecialStatus.Status : Ammo.HeavyStatus.Status, statusAmount = 1 });
		}
	}
}