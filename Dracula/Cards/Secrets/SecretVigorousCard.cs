using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SecretVigorousCard : SecretCard, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Vigorous", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Secret", "Vigorous", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
	{
		var data = base.GetData(state);
		data.flippable = true;
		return data;
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AMove
			{
				targetPlayer = true,
				dir = 2
			}
		];
}
