using Shockah.Shared;
using System.Linq;

namespace Shockah.Soggins;

public sealed class ASogginsExe : ACardOffering
{
	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		timer = 0.0;

		var cards = CardReward.GetOffering(s, amount, limitDeck, battleType ?? BattleType.Normal, rarityOverride, overrideUpgradeChances, makeAllCardsTemporary, inCombat, discount, isEvent);
		if (!cards.Any(c => c is SmugnessControlCard or HarnessingSmugnessCard))
		{
			Card replacementCard = s.rngActions.Chance(0.25) ? new HarnessingSmugnessCard() : new SmugnessControlCard();
			replacementCard.drawAnim = 1;
			replacementCard.flipAnim = 1;
			replacementCard.temporaryOverride = makeAllCardsTemporary;
			replacementCard.discount = discount;
			cards[s.rngActions.NextInt() % cards.Count] = replacementCard;
		}

		return new CardReward
		{
			cards = cards,
			canSkip = canSkip
		};
	}
}
