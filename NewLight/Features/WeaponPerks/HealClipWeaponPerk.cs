using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class HealClipWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/HealClip.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("HealClip", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "HealClip", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::HealClip")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "HealClip", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "HealClip", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = _ => true;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Solar;
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				combat.HealClipCardsPlayedThisTurn.Add(card.uuid);
		});
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnEnd), (Combat combat) =>
		{
			var healClipCardsPlayedThisTurn = combat.HealClipCardsPlayedThisTurn;
			if (healClipCardsPlayedThisTurn.Count == 0)
				return;
			
			if (combat.energy > 0)
				combat.QueueImmediate(new AHeal { targetPlayer = true, healAmount = healClipCardsPlayedThisTurn.Count });
			healClipCardsPlayedThisTurn.Clear();
		});
	}
}

file static class HealClipWeaponPerkExt
{
	extension(Combat combat)
	{
		public HashSet<int> HealClipCardsPlayedThisTurn
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "HealClipCardsPlayedThisTurn");
	}
}