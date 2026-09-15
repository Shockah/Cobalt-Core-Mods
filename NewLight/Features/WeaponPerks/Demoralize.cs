using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class DemoralizeWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	private static AAttack? AttackContext;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var activeIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/DemoralizeActive.png"));
		var inactiveIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/DemoralizeInactive.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Demoralize", new()
		{
			Icon = (state, card) => card is not null && (MG.inst.g.state ?? state).route is Combat combat && combat.TriggeredDemoralizeCards.Contains(card.uuid) ? inactiveIcon.Sprite : activeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Demoralize", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Demoralize")
				{
					Icon = activeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Demoralize", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Demoralize", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Void;
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Postfix))
		);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_NormalDamage_Postfix(Ship __instance, State s, Combat c, int? maybeWorldGridX)
	{
		if (AttackContext is null)
			return;
		if (maybeWorldGridX is not { } worldGridX)
			return;
		if (__instance.isPlayerShip)
			return;
		if (__instance.GetPartAtWorldX(worldGridX) is not { } part)
			return;

		var damageModifier = part.GetDamageModifier();
		if (damageModifier is not (PDamMod.brittle or PDamMod.weak))
			return;
		
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(s, AttackContext) is not { } sourceCard)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, sourceCard, Trait))
			return;

		var newTargetWorldX = worldGridX - 1;
		var newTargetPart = __instance.GetPartAtWorldX(newTargetWorldX);
		var newTargetPartDamageModifier = newTargetPart?.GetDamageModifier();
		if (newTargetPart is null || newTargetPart.type == PType.empty || newTargetPartDamageModifier == PDamMod.brittle || (newTargetPartDamageModifier == PDamMod.weak && damageModifier == PDamMod.brittle))
		{
			newTargetWorldX = worldGridX + 1;
			newTargetPart = __instance.GetPartAtWorldX(newTargetWorldX);
			newTargetPartDamageModifier = newTargetPart?.GetDamageModifier();
		}
		if (newTargetPart is null || newTargetPart.type == PType.empty || newTargetPartDamageModifier == PDamMod.brittle || (newTargetPartDamageModifier == PDamMod.weak && damageModifier == PDamMod.brittle))
			return;
		
		if (!c.TriggeredDemoralizeCards.Add(sourceCard.uuid))
			return;

		c.QueueImmediate(
			damageModifier == PDamMod.brittle
				? new ABrittle { targetPlayer = false, worldX = newTargetWorldX }
				: new AWeaken { targetPlayer = false, worldX = newTargetWorldX }
		);
	}
}

file static class DemoralizeWeaponPerkExt
{
	extension(Combat combat)
	{
		public HashSet<int> TriggeredDemoralizeCards
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "TriggeredDemoralizeCards");
	}
}