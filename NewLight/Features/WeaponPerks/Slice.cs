using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class SliceWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;

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
				},
				.. Severed.GetTooltips(),
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

				attack.Sever = true;
			}
		}
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
}