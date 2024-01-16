using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class SecretViolentCard : SecretCard, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Secret.Violent", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Secret", "Violent", "name"]).Localize
		});
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = GetDmg(s, 2)
			}
		];
}
