using System.Collections.Generic;
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
			Icon = (state, card) => card is not null && (MG.inst.g.state ?? state).route is Combat combat && combat.FullAutoCardsPlayedThisTurn.Contains(card.uuid) ? inactiveIcon.Sprite : activeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "FullAuto", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::FullAuto")
				{
					Icon = activeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "FullAuto", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "FullAuto", "Description"]),
				}
			]
		});
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			if (!combat.FullAutoCardsPlayedThisTurn.Add(card.uuid))
				return;
			
			card.discount--;
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (Combat combat) =>
		{
			combat.FullAutoCardsPlayedThisTurn.Clear();
		});
	}
}

file static class FullAutoCardTraitExt
{
	extension(Combat combat)
	{
		public HashSet<int> FullAutoCardsPlayedThisTurn
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "FullAutoCardsPlayedThisTurn");
	}
}