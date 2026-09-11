using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class FullAutoCardTrait : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var activeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/FullAutoActive.png"));
		var inactiveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/FullAutoInactive.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("FullAuto", new()
		{
			Icon = (_, card) => card?.FullAutoActivatedThisTurn == true ? inactiveIcon.Sprite : activeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "FullAuto", "Name"]).Localize,
			Tooltips = (_, card) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::FullAuto")
				{
					Icon = card?.FullAutoActivatedThisTurn == true ? inactiveIcon.Sprite : activeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "FullAuto", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "FullAuto", "Description"]),
				}
			]
		});
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (State state, Card card) =>
		{
			if (card.FullAutoActivatedThisTurn)
				return;
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			
			card.discount--;
			card.FullAutoActivatedThisTurn = true;
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
				card.FullAutoActivatedThisTurn = false;
		});
	}
}

file static class FullAutoCardTraitExt
{
	extension(Card card)
	{
		public bool FullAutoActivatedThisTurn
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(card, "FullAutoActivatedThisTurn");
			set
			{
				if (value)
					ModEntry.Instance.Helper.ModData.SetModData(card, "FullAutoActivatedThisTurn", true);
				else
					ModEntry.Instance.Helper.ModData.RemoveModData(card, "FullAutoActivatedThisTurn");
			}
		}
	}
}