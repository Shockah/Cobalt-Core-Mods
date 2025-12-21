using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class TearCard : Card, IDraculaCard, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Tear", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_WeakenHull,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Tear", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Independent.Trait };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.StatusLogic.MakeTriggerStatusAction(false, ModEntry.Instance.BleedingStatus.Status).AsCardAction,
				ModEntry.Instance.KokoroApi.StatusLogic.MakeTriggerStatusAction(false, ModEntry.Instance.BleedingStatus.Status).AsCardAction,
				ModEntry.Instance.KokoroApi.StatusLogic.MakeTriggerStatusAction(false, ModEntry.Instance.BleedingStatus.Status).AsCardAction,
				new AAttack { damage = GetDmg(s, 0), status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.StatusLogic.MakeTriggerStatusAction(false, ModEntry.Instance.BleedingStatus.Status).AsCardAction,
				new AAttack { damage = GetDmg(s, 0), status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 3 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.StatusLogic.MakeTriggerStatusAction(false, ModEntry.Instance.BleedingStatus.Status).AsCardAction,
				new AAttack { damage = GetDmg(s, 0), status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			]
		};
}