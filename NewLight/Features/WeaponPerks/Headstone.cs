using System.Collections.Generic;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class HeadstoneWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	private static readonly Geode TooltipGeode = new();
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var activeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/HeadstoneActive.png"));
		var inactiveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/HeadstoneInactive.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Headstone", new()
		{
			Icon = (state, card) => card is not null && (MG.inst.g.state ?? state).route is Combat combat && combat.HeadstoneTriggersThisTurn.Contains(card.uuid) ? inactiveIcon.Sprite : activeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Headstone", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Headstone")
				{
					Icon = activeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Headstone", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Headstone", "Description"]),
				},
				.. TooltipGeode.GetTooltips(),
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Stasis;
		
		Crits.Instance.Register(new CritHook(), 0);
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (Combat combat) =>
		{
			combat.HeadstoneTriggersThisTurn.Clear();
		});
	}

	private sealed class CritHook : Crits.IHook
	{
		public void OnCrit(Crits.IHook.OnCritArgs args)
		{
			if (args.Ship.isPlayerShip)
				return;
			if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(args.State, args.Attack) is not { } sourceCard)
				return;
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, sourceCard, Trait))
				return;
			if (!args.Combat.HeadstoneTriggersThisTurn.Add(sourceCard.uuid))
				return;
			
			args.Combat.Queue(new ASpawn { fromPlayer = true, fromX = args.Ship.x + args.LocalX - args.State.ship.x, thing = new Geode { yAnimation = 0 } });
		}
	}
}

file static class HeadstoneWeaponPerkExt
{
	extension(Combat combat)
	{
		public HashSet<int> HeadstoneTriggersThisTurn
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "HeadstoneTriggersThisTurn");
	}
}