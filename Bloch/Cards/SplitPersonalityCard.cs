using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class SplitPersonalityCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Payback,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/SplitPersonality.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SplitPersonality", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				targetPlayer = true,
				status = SplitPersonalityManager.SplitPersonalityStatus.Status,
				statusAmount = upgrade == Upgrade.B ? 3 : 1
			}
		];
}
