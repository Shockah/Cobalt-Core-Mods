using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class TearWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Tear.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Tear", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Tear", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Tear")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Tear", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Tear", "Description"]),
				},
				.. Severed.GetTooltips(),
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Strand;
		
		Crits.Instance.Register(new CritHook(), 0);
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
			
			if (args.Ship.GetPartAtLocalX(args.LocalX - 1) is { } leftPart && leftPart.type != PType.empty)
				leftPart.Severed = true;
			if (args.Ship.GetPartAtLocalX(args.LocalX + 1) is { } rightPart && rightPart.type != PType.empty)
				rightPart.Severed = true;
		}
	}
}