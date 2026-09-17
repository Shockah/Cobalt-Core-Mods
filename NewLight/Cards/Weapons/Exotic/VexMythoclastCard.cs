using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class VexMythoclastCard : ExoticWeaponCard, IHasCustomCardTraits, IRegisterable
{
	public static IStatusEntry Status { get; private set; } = null!;
	
	protected override WeaponElement WeaponElement => WeaponElement.Solar;

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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "VexMythoclast", "Name"]).Localize,
		});
		
		Status = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("VexMythoclast", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/VexMythoclast.png")).Sprite,
				color = new("7AF48B"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "VexMythoclast", "Status", "Name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Weapon", "Exotic", "VexMythoclast", "Status", "Description"]).Localize,
		});
		var statusCostSatisfiedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/VexMythoclastCostSatisfied.png"));
		var statusCostUnsatisfiedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Statuses/VexMythoclastCostUnsatisfied.png"));
		ModEntry.Instance.KokoroApi.ActionCosts.RegisterStatusResourceCostIcon(Status.Status, statusCostSatisfiedIcon.Sprite, statusCostUnsatisfiedIcon.Sprite);
		
		ModEntry.Instance.KokoroApi.Finite.SetBaseFiniteUses(entry.UniqueName, 3);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => base.GetData(state) with { cost = 1, floppable = true, recycle = true },
			_ => base.GetData(state) with { cost = 1, floppable = true },
		};

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry>
		{
			FullAutoCardTrait.Trait,
			ModEntry.Instance.KokoroApi.Finite.Trait,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 1), disabled = flipped },
				new AStatus { targetPlayer = true, status = Status.Status, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.Status), 2
					),
					new AAttack { damage = GetDmg(s, 4) }
				).AsCardAction.Disabled(!flipped),
			],
			_ => [
				new AAttack { damage = GetDmg(s, 1), disabled = flipped },
				new AStatus { targetPlayer = true, status = Status.Status, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.Status), 3
					),
					new AAttack { damage = GetDmg(s, 4) }
				).AsCardAction.Disabled(!flipped),
			],
		};
}