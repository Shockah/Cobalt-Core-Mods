using System.Collections.Generic;

namespace parchmentArmada.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
    internal class TrinityStarterCard : Card
    {
        internal static Spr card_sprite = StableSpr.cards_Shield;
        public override List<CardAction> GetActions(State s, Combat c)
        {
            var list = new List<CardAction>();
            switch (this.upgrade)
            {
                case Upgrade.None:
                    list.Add(new CardActions.ATrinityActivateNext() { amount = 1, disabled = flipped });
                    list.Add(new ADummyAction());
                    list.Add(new AStatus() { targetPlayer = true, status = Status.shield, statusAmount = 1, disabled = !flipped });
                    break;

                case Upgrade.A:
                    list.Add(new CardActions.ATrinityActivateNext() { amount = 1, disabled = flipped });
                    list.Add(new ADummyAction());
                    list.Add(new AStatus() { targetPlayer = true, status = Status.tempShield, statusAmount = 2, disabled = !flipped });
                    break;

                case Upgrade.B:
                    list.Add(new CardActions.ATrinityActivateNext() { amount = 2 });
                    list.Add(new AStatus() { targetPlayer = true, status = Status.shield, statusAmount = 1 });
                    break;
            }

            return list;
        }

        public override CardData GetData(State state) => new CardData
        {
            cost = upgrade == Upgrade.B ? 2 : 1,
            floppable = upgrade == Upgrade.B ? false : true,
            retain = upgrade == Upgrade.A ? true : false,
            art = upgrade == Upgrade.B ? StableSpr.cards_BigShield : (flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top)
        };

        public override string Name() => "Assault Bracing";
    }
}
