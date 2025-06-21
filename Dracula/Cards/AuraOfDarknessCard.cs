using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class AuraOfDarknessCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("AuraOfDarkness", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "AuraOfDarkness", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, recycle = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AHurt { targetPlayer = true, hurtAmount = 1 },
				new AHurt { targetPlayer = false, hurtAmount = 1 },
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			],
			Upgrade.A => [
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			]
		};
}
