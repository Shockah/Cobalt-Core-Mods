using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class TearWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	private static AAttack? AttackContext;
	
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
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Strand;
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(IntentAttack), nameof(IntentAttack.Apply)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(IntentAttack_Apply_Prefix_VeryHigh)), priority: Priority.VeryHigh),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(IntentAttack_Apply_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(IntentAttack), nameof(IntentAttack.GetOtherRenderStuff)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(IntentAttack_GetOtherRenderStuff_Prefix_VeryHigh)), priority: Priority.VeryHigh),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(IntentAttack_GetOtherRenderStuff_Finalizer))
		);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s, Combat c, int? maybeWorldGridX)
	{
		if (AttackContext is null)
			return;
		if (AttackContext.targetPlayer)
			return;
		if (__instance == s.ship)
			return;
		if (maybeWorldGridX is not { } worldGridX)
			return;
		if (__instance.GetPartAtWorldX(worldGridX) is not { } part)
			return;
		if (part.GetDamageModifier() is not (PDamMod.brittle or PDamMod.weak))
			return;
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(s, AttackContext) is not { } sourceCard)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, sourceCard, Trait))
			return;

		if (__instance.GetPartAtWorldX(worldGridX - 1) is { } leftPart && leftPart.type != PType.empty)
			leftPart.Severed = true;
		if (__instance.GetPartAtWorldX(worldGridX + 1) is { } rightPart && rightPart.type != PType.empty)
			rightPart.Severed = true;
	}

	private static void IntentAttack_Apply_Prefix_VeryHigh(IntentAttack __instance, Ship fromShip, int actualX, out int __state)
	{
		__state = __instance.damage;
		if (fromShip.GetPartAtLocalX(actualX) is not { } part)
			return;
		if (!part.Severed)
			return;
		__instance.damage--;
	}

	private static void IntentAttack_Apply_Finalizer(IntentAttack __instance, in int __state)
		=> __instance.damage = __state;

	private static void IntentAttack_GetOtherRenderStuff_Prefix_VeryHigh(IntentAttack __instance, State s, out (Part? part, int damage) __state)
	{
		__state = (null, 0);
		
		if (s.route is not Combat combat)
			return;

		foreach (var part in combat.otherShip.parts)
		{
			if (part.intent != __instance)
				continue;
			if (!part.Severed)
				continue;
			__state = (part, __instance.damage);
			__instance.damage--;
		}
	}

	private static void IntentAttack_GetOtherRenderStuff_Finalizer(IntentAttack __instance, in (Part? part, int damage) __state)
	{
		if (__state.part is null)
			return;
		__instance.damage = __state.damage;
	}
}