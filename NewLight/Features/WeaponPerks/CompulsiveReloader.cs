using System;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class CompulsiveReloaderWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/CompulsiveReloader.png"));

		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("CompulsiveReloader", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "CompulsiveReloader", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::CompulsiveReloader")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "CompulsiveReloader", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "CompulsiveReloader", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is WeaponCard.IUsesAmmo and not WeaponCard.IEnergyFree;

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetCurrentCost)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetCurrentCost_Postfix))
		);
	}

	private static void Card_GetCurrentCost_Postfix(Card __instance, State s, ref int __result)
	{
		if (s.route is not Combat combat)
			return;

		var specialCost = Ammo.GetSpecialCost(s, combat, __instance);
		var heavyCost = Ammo.GetHeavyCost(s, combat, __instance);
		if (specialCost is null && heavyCost is null)
			return;
		
		if (specialCost is not null && s.ship.Get(Ammo.SpecialStatus.Status) < 5)
			return;
		if (heavyCost is not null && s.ship.Get(Ammo.HeavyStatus.Status) < 5)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;

		__result = Math.Max(__result - 1, 0);
	}
}