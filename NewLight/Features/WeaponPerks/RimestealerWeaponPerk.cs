using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class RimestealerWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	private static AAttack? AttackContext;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Rimestealer.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Rimestealer", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Rimestealer", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Rimestealer")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Rimestealer", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Rimestealer", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Stasis;
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DestroyDroneAt)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Postfix))
		);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Combat_DestroyDroneAt_Prefix(Combat __instance, int x, out StuffBase? __state)
		=> __state = __instance.stuff.GetValueOrDefault(x);

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, State s, int x, bool playerDidIt, in StuffBase? __state)
	{
		if (!playerDidIt)
			return;
		if (__state is not Geode)
			return;
		if (AttackContext is null)
			return;
		if (AttackContext.targetPlayer)
			return;
		if (__instance.stuff.GetValueOrDefault(x) == __state)
			return;
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(s, AttackContext) is not { } sourceCard)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, sourceCard, Trait))
			return;
		
		__instance.Queue(new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 1 });
	}
}