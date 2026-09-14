using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class RepulsorBraceWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var activeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/RepulsorBraceActive.png"));
		var inactiveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/RepulsorBraceInactive.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("RepulsorBrace", new()
		{
			Icon = (state, card) => card is not null && (MG.inst.g.state ?? state).route is Combat combat && combat.RepulsorBraceCardsPlayedThisTurn.Contains(card.uuid) ? inactiveIcon.Sprite : activeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "RepulsorBrace", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::RepulsorBrace")
				{
					Icon = activeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "RepulsorBrace", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "RepulsorBrace", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = _ => true;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Void;
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (!helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			if (!combat.RepulsorBraceCardsPlayedThisTurn.Add(card.uuid))
				return;

			combat.Queue(new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 1 });
		});
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (Combat combat) =>
		{
			combat.RepulsorBraceCardsPlayedThisTurn.Clear();
		});
	}
}

file static class RepulsorBraceWeaponPerkExt
{
	extension(Combat combat)
	{
		public HashSet<int> RepulsorBraceCardsPlayedThisTurn
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "RepulsorBraceCardsPlayedThisTurn");
	}
}