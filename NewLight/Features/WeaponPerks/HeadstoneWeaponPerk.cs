using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class HeadstoneWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

	private static AAttack? AttackContext;
	
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
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Stasis;
		
		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (Combat combat) =>
		{
			combat.HeadstoneTriggersThisTurn.Clear();
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix))
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
		if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCardId(AttackContext) is not { } sourceCardId)
			return;
		if (!c.HeadstoneTriggersThisTurn.Add(sourceCardId))
			return;

		c.Queue(new ASpawn { fromPlayer = true, fromX = worldGridX - s.ship.x, thing = new Geode { yAnimation = 0 } });
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