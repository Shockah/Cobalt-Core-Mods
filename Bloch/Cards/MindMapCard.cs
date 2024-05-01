using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MindMapCard : Card, IRegisterable
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
			Art = StableSpr.cards_ThinkTwice,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/MindMap.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MindMap", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 1 : 2,
			exhaust = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				targetPlayer = true,
				status = RetainManager.RetainStatus.Status,
				statusAmount = upgrade == Upgrade.B ? 2 : 1
			}
		];
}
