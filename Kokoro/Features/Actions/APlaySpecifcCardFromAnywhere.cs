using HarmonyLib;
using Newtonsoft.Json;
using Shockah.Shared;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class APlaySpecificCardFromAnywhere : CardAction
{
	private static ModEntry Instance => ModEntry.Instance;

	private static Card? CardToRemoveFromHand;

	public int CardId;
	public bool ShowTheCardIfNotInHand = true;

	[JsonProperty]
	private bool Recursive = false;

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.PlayerCanAct)),
			postfix: new HarmonyMethod(typeof(APlaySpecificCardFromAnywhere), nameof(Combat_PlayerCanAct_Postfix))
		);
	}

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		Card? card = c.hand.Concat(s.deck).Concat(c.discard).Concat(c.exhausted).FirstOrDefault(c => c.uuid == CardId);
		if (card is null)
			return;

		void PlayCardAndFixQueue()
		{
			if (card is null)
				return;

			var queue = c.cardActions.Where(a => a != this).ToList();
			c.cardActions.Clear();
			c.TryPlayCard(s, card, playNoMatterWhatForFree: true);
			c.cardActions.AddRange(queue);
		}

		if (c.hand.Contains(card))
		{
			if (Recursive)
				CardToRemoveFromHand = card;
			PlayCardAndFixQueue();
			return;
		}

		if (!ShowTheCardIfNotInHand)
		{
			CardToRemoveFromHand = card;
			c.hand.Add(card);
			PlayCardAndFixQueue();
			return;
		}

		s.RemoveCardFromWhereverItIs(CardId);

		var handCopy = c.hand.ToList();
		c.hand.Clear();
		c.SendCardToHand(s, card);
		c.hand.InsertRange(0, handCopy);

		c.QueueImmediate(new APlaySpecificCardFromAnywhere { CardId = card.uuid, Recursive = true });
		c.QueueImmediate(new ADelay() { time = -0.2 });
	}

	private static void Combat_PlayerCanAct_Postfix(Combat __instance)
	{
		if (CardToRemoveFromHand is null)
			return;
		__instance.hand.Remove(CardToRemoveFromHand);
		CardToRemoveFromHand = null;
	}
}
