using System.Collections.Generic;

namespace parchmentArmada.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { })]
    internal class ErisAnerisCard : Card
    {
        public override List<CardAction> GetActions(State s, Combat c)
        {
            var list = new List<CardAction>();
            list.Add(new CardActions.AErisDestroyAllStrife() { });
            return list;
        }

        public override CardData GetData(State state) => new CardData
        {
            cost = 0,
            art = StableSpr.cards_BlockerBurnout,
            temporary = true,
            exhaust = true,
            retain = true,
        };
        public override string Name() => "Aneris";

    }
}
