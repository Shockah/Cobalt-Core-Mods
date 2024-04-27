using System.Collections.Generic;

namespace parchmentArmada.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
    internal class ProteusMissileCard : Card
    {

        public override List<CardAction> GetActions(State s, Combat c)
        { 
            var list = new List<CardAction>();
            switch (this.upgrade)
            {
                
                case Upgrade.None:
                    list.Add(new ASpawn() { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal }} );
                    list.Add(new AStatus() { targetPlayer = true, status = Status.drawNextTurn, statusAmount = 1 });
                    break;

                case Upgrade.B:
                    list.Add(new ASpawn() { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy } });
                    list.Add(new AStatus() { targetPlayer = true, status = Status.drawNextTurn, statusAmount = 3 });
                    break;

                case Upgrade.A:
                    list.Add(new ASpawn() { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal }, disabled=flipped });
                    list.Add(new ADummyAction());
                    list.Add(new AStatus() { targetPlayer = true, status = Status.drawNextTurn, statusAmount = 2, disabled=!flipped });
                    break;
            }
            return list;
        }

        public override CardData GetData(State state) => new CardData
        {
            cost = upgrade == Upgrade.A ? 0 : (upgrade == Upgrade.B ? 2 : 1),
            floppable = upgrade == Upgrade.A ? true : false,
            art = upgrade != Upgrade.A ? StableSpr.cards_SeekerMissileCard : (flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top)
        };
        public override string Name() => "Eject Waste";
    }
}
