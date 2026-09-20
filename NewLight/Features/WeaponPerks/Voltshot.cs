using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.NewLight;

internal sealed class VoltshotWeaponPerk : IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/Voltshot.png"));
		var primedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/WeaponPerks/VoltshotPrimed.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Voltshot", new()
		{
			Icon = (state, card) => card is not null && (MG.inst.g.state ?? state).route is Combat combat && combat.VoltshotPrimed.Contains(card.uuid) ? primedIcon.Sprite : icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["CardTrait", "WeaponPerk", "Voltshot", "Name"]).Localize,
			Tooltips = (_, _) =>
			[
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Voltshot")
				{
					Icon = icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Voltshot", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["CardTrait", "WeaponPerk", "Voltshot", "Description"]),
				},
				.. Jolted.GetTooltips(false),
			]
		});

		LegendaryWeaponCard.WeaponPerkConditions[Trait.UniqueName] = weapon => weapon is not WeaponCard.ICannotCrit;
		LegendaryWeaponCard.WeaponPerkElementAssignments[Trait.UniqueName] = WeaponElement.Arc;
		
		Crits.Instance.Register(new CritHook(), 0);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix_VeryHigh)), priority: Priority.VeryHigh)
		);
	}

	private static void Card_GetActionsOverridden_Postfix_VeryHigh(Card __instance, State s, Combat c, ref List<CardAction> __result)
	{
		if (!c.VoltshotPrimed.Contains(__instance.uuid))
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;

		foreach (var baseAction in __result)
		{
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(baseAction))
			{
				if (wrappedAction is not AAttack attack)
					continue;

				attack.Jolt = true;
			}
		}

		__result.Add(ModEntry.Instance.KokoroApi.HiddenActions.MakeAction(new UnprimeAction { CardId = __instance.uuid }).AsCardAction);
	}

	private sealed class UnprimeAction : CardAction
	{
		public required int CardId;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			c.VoltshotPrimed.Remove(CardId);
		}
	}

	private sealed class CritHook : Crits.IHook
	{
		public void OnCrit(Crits.IHook.OnCritArgs args)
		{
			if (args.Ship.isPlayerShip)
				return;
			if (ModEntry.Instance.KokoroApi.ActionInfo.GetSourceCard(args.State, args.Attack) is not { } sourceCard)
				return;
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, sourceCard, Trait))
				return;

			args.Combat.VoltshotPrimed.Add(sourceCard.uuid);
		}
	}
}

file static class HeadstoneWeaponPerkExt
{
	extension(Combat combat)
	{
		public HashSet<int> VoltshotPrimed
			=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<int>>(combat, "VoltshotPrimed");
	}
}