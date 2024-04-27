using System.Collections.Generic;

namespace parchmentArmada.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { })]
    internal class TrinityRagnarok : Card
    {
        public override List<CardAction> GetActions(State s, Combat c)
        {
            var list = new List<CardAction>();
            list.Add(new CardActions.ATrinityActivateNext() { amount = 3 });

            return list;
        }

        public override CardData GetData(State state) => new CardData
        {
            cost = 1,
            retain = true,
            temporary = true,
            exhaust = true,
            art = StableSpr.cards_WaveBeam
        };

        public override string Name() => "Ragnarok";
    }
}
