using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class ExplosiveManager : IRegisterable
{
	internal static ICardTraitEntry ExplosiveTrait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Traits/Explosive.png"));
		
		ExplosiveTrait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Explosive", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Explosive"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Explosive")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.action,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Spontaneous", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Spontaneous", "description"]),
				}
			]
		});
		
		ModEntry.Instance.Helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ExplosiveTrait))
				return;
			
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(state, card, ExplosiveTrait, false, permanent: false);
			combat.Queue(new AAttack { damage = card.GetDmg(state, 5) });
		});
	}
}