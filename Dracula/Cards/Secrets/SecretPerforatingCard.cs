using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SecretPerforatingCard : SecretCard, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Perforating", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A],
				dontOffer = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Secret", "Perforating", "name"]).Localize
		});
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = GetDmg(s, 1),
				status = ModEntry.Instance.BleedingStatus.Status,
				statusAmount = 2
			}
		];
}
