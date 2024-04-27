using System.Collections.Generic;


namespace parchmentArmada.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, dontOffer = true, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
    internal class HeliosHealCard : Card
    {
        public override List<CardAction> GetActions(State s, Combat c)
        {
            var list = new List<CardAction>();
            switch (this.upgrade)
            {
                case Upgrade.None:
                    list.Add(new AHeal() { targetPlayer = true, healAmount = 1, canRunAfterKill = true });
                    break;

                case Upgrade.A:
                    list.Add(new AHeal() { targetPlayer = true, healAmount = 1, canRunAfterKill = true });
                    break;

                case Upgrade.B:
                    list.Add(new AHeal() { targetPlayer = true, healAmount = 2, canRunAfterKill = true });
                    break;
            }

            return list;
        }

        public override CardData GetData(State state) => new CardData
        {
            cost = upgrade == Upgrade.B ? 3 : upgrade == Upgrade.A ? 1 : 2,
            exhaust = true,
            art = StableSpr.cards_ShieldSurge,
            artTint = "38ff94"

        };

        public override string Name() => "Basic Repair";
    }
}
