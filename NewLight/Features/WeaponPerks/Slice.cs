using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class SliceWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	private static AAttack? AttackContext;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Slice.png"));

		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Slice", new()
		{
			Icon = (_, _) => icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Slice", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Slice")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Slice", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Slice", "Description"]),
				}
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Strand;
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state, Combat combat) =>
		{
			if (Abilities.GetCooldown(state, combat, card) <= 0)
				return;
			combat.SliceThisTurn = true;
		});
		
		helper.Events.RegisterAfterArtifactsHook(nameof(Artifact.OnTurnEnd), (Combat combat) =>
		{
			combat.SliceThisTurn = false;
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix_VeryHigh)), priority: Priority.VeryHigh)
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix)), priority: Priority.Low)
		);
	}

	private static void Card_GetActionsOverridden_Postfix_VeryHigh(Card __instance, State s, Combat c, ref List<CardAction> __result)
	{
		if (!c.SliceThisTurn)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;

		foreach (var baseAction in __result)
		{
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(baseAction))
			{
				if (wrappedAction is not AAttack attack)
					continue;

				attack.Slice = true;
			}
		}
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s, Combat c, int? maybeWorldGridX)
	{
		if (AttackContext is null)
			return;
		if (maybeWorldGridX is not { } worldGridX)
			return;
		if (__instance.GetPartAtWorldX(worldGridX) is not { } part || part.type == PType.empty)
			return;
		if (!AttackContext.Slice)
			return;

		part.Severed = true;
	}
}

file static class SliceWeaponPerkExt
{
	extension(Combat combat)
	{
		public bool SliceThisTurn
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(combat, "SliceThisTurn");
			set
			{
				if (value)
					ModEntry.Instance.Helper.ModData.SetModData(combat, "SliceThisTurn", true);
				else
					ModEntry.Instance.Helper.ModData.RemoveModData(combat, "SliceThisTurn");
			}
		}
	}
	
	extension(AAttack attack)
	{
		public bool Slice
		{
			get => ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(attack, "Slice");
			set => ModEntry.Instance.Helper.ModData.SetModData(attack, "Slice", value);
		}
	}
}