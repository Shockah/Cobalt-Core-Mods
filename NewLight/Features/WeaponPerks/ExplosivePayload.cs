using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class ExplosivePayloadWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/ExplosivePayload.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("ExplosivePayload", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "ExplosivePayload", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::ExplosivePayload")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ExplosivePayload", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "ExplosivePayload", "Description"]),
				}
			]
		});
		
		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit && weapon is not WeaponCard.IZeroDamageAttacks;
		LegendaryWeaponCard.DamageWeaponPerks.Add(Trait.UniqueName);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix_VeryHigh)), priority: Priority.VeryHigh)
		);
	}

	private static void Card_GetActionsOverridden_Postfix_VeryHigh(Card __instance, State s, ref List<CardAction> __result)
	{
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;

		foreach (var baseAction in __result)
		{
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(baseAction))
			{
				if (wrappedAction is not AAttack attack)
					continue;
				
				attack.damage = Math.Max(attack.damage - 1, 0);
				attack.BlastDamage = 1;
				attack.BlastRange = 1;
				attack.HideBlastInCardRendering = true;
				attack.CanPrimaryBlastDamageCrit = true;
			}
		}
	}
}