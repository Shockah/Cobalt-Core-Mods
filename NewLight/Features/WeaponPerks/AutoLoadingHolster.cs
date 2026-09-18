using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class AutoLoadingHolsterWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/AutoLoadingHolster.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("AutoLoadingHolster", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "AutoLoadingHolster", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::AutoLoadingHolster")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "AutoLoadingHolster", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "AutoLoadingHolster", "Description"]),
				}
			]
		});
		
		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is WeaponCard.IUsesAmmo;
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToHand_Postfix))
		);
	}

	private static void Combat_SendCardToHand_Postfix(Combat __instance, State s, Card card)
	{
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Trait))
			return;
		
		if (Ammo.GetHeavyCost(s, __instance, card) is not null)
			__instance.QueueImmediate(new AStatus { targetPlayer = true, status = Ammo.HeavyStatus.Status, statusAmount = 1 });
		else if (Ammo.GetSpecialCost(s, __instance, card) is not null)
			__instance.QueueImmediate(new AStatus { targetPlayer = true, status = Ammo.SpecialStatus.Status, statusAmount = 1 });
	}
}