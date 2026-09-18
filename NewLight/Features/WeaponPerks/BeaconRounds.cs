using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class BeaconRoundsWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/BeaconRounds.png"));

		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("BeaconRounds", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "BeaconRounds", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::BeaconRounds")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "BeaconRounds", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "BeaconRounds", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = _ => true;

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetFromX)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetFromX_Postfix))
		);
	}

	private static void AAttack_GetFromX_Postfix(AAttack __instance, State s, ref int? __result)
	{
		if (__instance.fromDroneX is not null)
			return;
		if (__result is not { } localX)
			return;
		if (s.route is not Combat combat)
			return;

		var source = __instance.targetPlayer ? combat.otherShip : s.ship;
		var target = __instance.targetPlayer ? s.ship : combat.otherShip;
		if (target.GetPartAtWorldX(localX + source.x) is { } part && part.type != PType.empty)
			return;
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(s, __instance) is not { } sourceCard)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, sourceCard, Trait))
			return;

		if (target.GetPartAtWorldX(localX + source.x - 1) is { } leftPart && leftPart.type != PType.empty)
			__result = localX - 1;
		else if (target.GetPartAtWorldX(localX + source.x + 1) is { } rightPart && rightPart.type != PType.empty)
			__result = localX + 1;
	}
}