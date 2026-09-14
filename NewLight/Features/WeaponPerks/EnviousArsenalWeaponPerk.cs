using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class EnviousArsenalWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/EnviousArsenal.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("EnviousArsenal", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "EnviousArsenal", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::EnviousArsenal")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "EnviousArsenal", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "EnviousArsenal", "Description"]),
				}
			]
		});
		
		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.IEnergyFree;
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (card is not WeaponCard)
				return;

			foreach (var handCard in combat.hand)
			{
				if (handCard == card)
					continue;
				if (!helper.Content.Cards.IsCardTraitActive(state, handCard, Trait))
					continue;

				handCard.discount--;
			}
		});
	}
}