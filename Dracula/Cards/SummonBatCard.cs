using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SummonBatCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("SummonBat", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/SummonBat.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SummonBat", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ASpawn
			{
				thing = new BatStuff
				{
					targetPlayer = false,
					yAnimation = 1,
					Type = upgrade switch
					{
						Upgrade.A => BatStuff.BatType.Bloodthirsty,
						Upgrade.B => BatStuff.BatType.Protective,
						_ => BatStuff.BatType.Normal
					},
					bubbleShield = upgrade == Upgrade.B
				}
			}
		];
}
