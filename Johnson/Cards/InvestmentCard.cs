using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class InvestmentCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohnsonDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Investment", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top,
			cost = upgrade == Upgrade.B ? 0 : 1,
			floppable = true,
			recycle = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = GetDmg(s, upgrade == Upgrade.B ? 0 : 1),
				disabled = flipped
			},
			new ADummyAction(),
			new AStrengthen
			{
				CardId = uuid,
				Amount = 2,
				disabled = !flipped
			}
		];
}
