using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class DeadMansTaleCard : ExoticWeaponCard, IHasCustomCardTraits, IRegisterable
{
	public static ICardTraitEntry Trait { get; private set; } = null!;
	
	protected override WeaponElement WeaponElement => WeaponElement.Kinetic;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "DeadMansTale", "Name"]).Localize,
		});
		
		var traitIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/CardTraits/ExoticWeaponPerks/DeadMansTale.png"));
		
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("DeadMansTale", new()
		{
			Icon = (_, _) => traitIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "DeadMansTale", "CardTrait", "Name"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::DeadMansTale")
				{
					Icon = traitIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["Card", "Weapon", "Exotic", "DeadMansTale", "CardTrait", "Name"]),
					Description = ModEntry.Instance.Localizations.Localize(["Card", "Weapon", "Exotic", "DeadMansTale", "CardTrait", "Description"]),
				}
			]
		});
		
		PrecisionCardTrait.SetPrecision(entry.UniqueName, 1);
		
		PrecisionCardTrait.Instance.Register(new PrecisionHook(), 0);
		Crits.Instance.Register(new CritHook(), 0);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 1, flippable = true },
			Upgrade.A => base.GetData(state) with { cost = 1, recycle = true },
			_ => base.GetData(state) with { cost = 1 },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { PrecisionCardTrait.Trait, Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AMove { targetPlayer = true, dir = 1 },
				new AAttack { damage = GetDmg(s, 1), piercing = true },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1), piercing = true },
			],
		};
	
	private sealed class PrecisionHook : PrecisionCardTrait.IHook
	{
		public void ModifyPrecision(ref PrecisionCardTrait.IHook.ModifyPrecisionArgs args)
		{
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, args.Card, Trait))
				return;

			args.Precision += args.Combat.DeadMansTaleBonusPrecision.GetValueOrDefault(args.Card.uuid);
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

			args.Combat.DeadMansTaleBonusPrecision[sourceCard.uuid] = args.Combat.DeadMansTaleBonusPrecision.GetValueOrDefault(sourceCard.uuid) + 1;
		}
	}
}

file static class DeadMansTaleCardExt
{
	extension(Combat combat)
	{
		public Dictionary<int, int> DeadMansTaleBonusPrecision
			=> ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<int, int>>(combat, "DeadMansTaleBonusPrecision");
	}
}