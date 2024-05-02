using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class OptCard : Card, IRegisterable
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
			Art = StableSpr.cards_QuickThinking,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Opt.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Opt", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 1 : 0,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ScryAction
			{
				Amount = upgrade switch
				{
					Upgrade.A => 2,
					Upgrade.B => 3,
					_ => 1
				}
			},
			new ADrawCard
			{
				count = upgrade == Upgrade.B ? 2 : 1
			}
		];
}
