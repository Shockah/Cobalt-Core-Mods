using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class ClonedLeechCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Leech", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Cannon,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Leech", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			exhaust = upgrade != Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = GetDmg(s, upgrade == Upgrade.A ? 3 : 2),
				piercing = true,
				stunEnemy = true
			},
			new AHeal
			{
				targetPlayer = true,
				healAmount = upgrade == Upgrade.A ? 2 : 1,
				canRunAfterKill = true
			}
		];
}
