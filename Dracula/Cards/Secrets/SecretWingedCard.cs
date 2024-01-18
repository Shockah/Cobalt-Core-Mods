using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SecretWingedCard : SecretCard, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Winged", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Secret", "Winged", "name"]).Localize
		});
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ASpawn
			{
				thing = new BatStuff
				{
					targetPlayer = false,
					yAnimation = 1
				}
			}
		];
}
