using Nickel;
using System.Collections.Generic;

namespace Shockah.Natasha;

public sealed class ApiImplementation : INatashaApi
{
	public IDeckEntry NatashaDeck
		=> ModEntry.Instance.NatashaDeck;

	public IStatusEntry ReprogrammedStatus
		=> Reprogram.ReprogrammedStatus;

	public IStatusEntry DeprogrammedStatus
		=> Reprogram.DeprogrammedStatus;

	public ICardTraitEntry LimitedTrait
		=> Limited.Trait;

	public int GetBaseLimitedUses(string key, Upgrade upgrade)
		=> Limited.GetBaseLimitedUses(key, upgrade);

	public void SetBaseLimitedUses(string key, int value)
		=> Limited.SetBaseLimitedUses(key, value);

	public void SetBaseLimitedUses(string key, Upgrade upgrade, int value)
		=> Limited.SetBaseLimitedUses(key, upgrade, value);

	public int GetStartingLimitedUses(State state, Card card)
		=> Limited.GetStartingLimitedUses(state, card);

	public int GetLimitedUses(State state, Card card)
		=> card.GetLimitedUses(state);

	public void SetLimitedUses(State state, Card card, int value)
		=> card.SetLimitedUses(value);

	public void ResetLimitedUses(State state, Card card)
		=> card.ResetLimitedUses();

	public CardAction MakeLimitedUsesVariableHintAction(int cardId)
		=> new LimitedUsesVariableHint { CardId = cardId };

	public CardAction MakeChangeLimitedUsesAction(int cardId, int amount, AStatusMode mode = AStatusMode.Add)
		=> new ChangeLimitedUsesAction { CardId = cardId, Amount = amount, Mode = mode };

	public ACardSelect SetFilterLimited(ACardSelect action, bool? limited)
		=> action.SetFilterLimited(limited);

	public CardAction MakeSequenceAction(int cardId, CardAction action, int sequenceStep, int sequenceLength)
		=> new SequenceAction { CardId = cardId, Action = action, SequenceStep = sequenceStep, SequenceLength = sequenceLength };

	public CardAction MakeOneLinerAction(List<CardAction> actions, int spacing = 3)
		=> new OneLinerAction { Actions = actions, Spacing = spacing };

	public void RegisterHook(INatashaHook hook, double priority)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(INatashaHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);

	public void RegisterManInTheMiddleStaticObject(INatashaApi.ManInTheMiddleStaticObjectEntry entry)
		=> ManInTheMiddleCard.RegisteredObjects[entry.UniqueName] = entry;
}
