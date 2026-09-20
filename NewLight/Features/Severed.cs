using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class Severed : IRegisterable
{
	public static CustomPartTraits.ITraitEntry Trait { get; private set; } = null!;
	private static ISpriteEntry Icon = null!;

	private static AAttack? AttackContext;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/PartTraits/Severed.png"));
		
		Trait = CustomPartTraits.RegisterTrait(package.Manifest, "Severed", new()
		{
			Icon = _ => Icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["PartTrait", "Severed", "Name"]).Localize,
			Tooltips = _ => GetTooltips(),
		});
		
		CustomPartTraits.Instance.Register(new CustomPartTraitsHook(), 0);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnAfterTurn_Postfix))
		);
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

	public static IEnumerable<Tooltip> GetTooltips()
		=> [
			new GlossaryTooltip($"parttrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Severed")
			{
				Icon = Icon.Sprite,
				TitleColor = Colors.parttrait,
				Title = ModEntry.Instance.Localizations.Localize(["PartTrait", "Severed", "Name"]),
				Description = ModEntry.Instance.Localizations.Localize(["PartTrait", "Severed", "Description"]),
			}
		];

	private static void Ship_OnAfterTurn_Postfix(Ship __instance)
	{
		foreach (var part in __instance.parts)
			part.Severed = false;
	}
	
	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s, int? maybeWorldGridX)
	{
		if (AttackContext is null)
			return;
		if (__instance == s.ship)
			return;
		if (maybeWorldGridX is not { } worldGridX)
			return;
		if (__instance.GetPartAtWorldX(worldGridX) is not { } part)
			return;
		if (!AttackContext.Sever)
			return;

		part.Severed = true;
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

	private sealed class CustomPartTraitsHook : CustomPartTraits.IHook
	{
		public void ModifyCustomPartTraits(CustomPartTraits.IHook.ModifyCustomPartTraitsArgs args)
		{
			if (!args.Part.Severed)
				return;
			args.Traits.Add(Trait);
		}
	}
}

public static class SeveredExt
{
	extension(AAttack attack)
	{
		public bool Sever
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(attack, "Sever");
			set
			{
				if (value)
					ModEntry.Instance.Helper.ModData.SetModData(attack, "Sever", true);
				else
					ModEntry.Instance.Helper.ModData.RemoveModData(attack, "Sever");
			}
		}
	}
	
	extension(Part part)
	{
		public bool Severed
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(part, "Severed");
			set
			{
				if (value)
					ModEntry.Instance.Helper.ModData.SetModData(part, "Severed", true);
				else
					ModEntry.Instance.Helper.ModData.RemoveModData(part, "Severed");
			}
		}
	}
}