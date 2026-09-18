using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class LeMonarqueCard : ExoticWeaponCard, IHasCustomCardTraits, IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	protected override WeaponElement WeaponElement => WeaponElement.Void;

	public new static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Weapon.png"), StableSpr.cards_Cannon).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "LeMonarque", "Name"]).Localize,
		});
		
		var traitIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/ExoticWeaponPerks/LeMonarque.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("LeMonarque", new()
		{
			Icon = (_, _) => traitIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "LeMonarque", "CardTrait", "Name"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::LeMonarque")
				{
					Icon = traitIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["Card", "Weapon", "Exotic", "LeMonarque", "CardTrait", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["Card", "Weapon", "Exotic", "LeMonarque", "CardTrait", "Description"]),
				}
			]
		});
		
		TensionCardTrait.SetTension(entry.UniqueName, Upgrade.None, 2);
		TensionCardTrait.SetTension(entry.UniqueName, Upgrade.A, 2);
		TensionCardTrait.SetTension(entry.UniqueName, Upgrade.B, 4);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(GetActionsOverridden)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix_VeryHigh)), priority: Priority.VeryHigh)
		);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 2, exhaust = true },
			_ => base.GetData(state) with { cost = 2 },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> upgrade switch
		{
			Upgrade.B => new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Heavy.Trait },
			_ => new HashSet<ICardTraitEntry> { Trait, ModEntry.Instance.KokoroApi.Heavy.Trait },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 2), status = Status.corrode, statusAmount = 1 },
			],
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 3) },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 2) },
			],
		};

	private static void Card_GetActionsOverridden_Postfix_VeryHigh(Card __instance, State s, Combat c, ref List<CardAction> __result)
	{
		if (!TensionCardTrait.IsTensioned(s, c, __instance))
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;

		foreach (var baseAction in __result)
		{
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(baseAction))
			{
				if (wrappedAction is not AAttack attack)
					continue;
				
				attack.status = Status.corrode;
				attack.statusAmount = 1;
			}
		}
	}
}