using System.Collections.Generic;

namespace parchmentArmada.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { })]
    internal class ProteusMutator : Card
        {
        public int posInHand = 1;
        public PType part;
        public string sprite = null!;
        private void getPosInHand(Combat c)
        {
            int tempPos = 0;
            bool flag = false;
            foreach (Card card in c.hand)
            {
                tempPos += 1;
                if (card.GetType() == typeof(Cards.ProteusMutator))
                {
                    flag = true;
                    break;
                }
            }
            if (flag) { posInHand = tempPos; }
            if (posInHand < 1) { posInHand = 0; }
            if (posInHand > 7) { posInHand = 0; }
        }

        public override List<CardAction> GetActions(State s, Combat c)
        {
            //getPosInHand(c);
            var list = new List<CardAction>();
            if (sprite != null) {
                list.Add(new CardActions.AProteusDisplayPart { skin = sprite });
                list.Add(new CardActions.AProteusMutate { pos = posInHand, skin = sprite, type = part });
            }
            //list.Add(new AAttack { damage = posInHand });
            return list;
        }

        public override void OnDiscard(State s, Combat c)
        {
            //c.Queue(new CardActions.AProteusMutate { pos = posInHand, skin = sprite, type = part });
            //c.SendCardToExhaust(s, this);
            //c.exhausted.Add(this);
        }

        public override void OnFlip(G g)
        {
            flipped = false;
            posInHand += 1;
            if(posInHand > 7) { posInHand = 1; }
        }
        
        public override CardData GetData(State state) => new CardData
        {
            cost = 0,
            art = new Spr?(StableSpr.cards_colorless),
            temporary = true,
            unplayable = true,
            flippable = true,
            exhaust = true
        };
        public override string Name() => "Reconfigure";
    }
}
