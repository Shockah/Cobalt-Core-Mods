using Nickel;

namespace Shockah.Bloch;

public sealed class ApiImplementation : IBlochApi
{
	public IDeckEntry BlochDeck
		=> ModEntry.Instance.BlochDeck;

	public IStatusEntry VeilingAuraStatus
		=> AuraManager.VeilingStatus;

	public IStatusEntry FeedbackAuraStatus
		=> AuraManager.FeedbackStatus;

	public IStatusEntry InsightAuraStatus
		=> AuraManager.InsightStatus;

	public IStatusEntry IntensifyStatus
		=> AuraManager.IntensifyStatus;

	public IStatusEntry DrawEachTurnStatus
		=> DrawEachTurnManager.DrawEachTurnStatus;

	public IStatusEntry MindMapStatus
		=> RetainManager.RetainStatus;

	public IStatusEntry PersonalitySplitStatus
		=> SplitPersonalityManager.SplitPersonalityStatus;

	public ICardTraitEntry SpontaneousTriggeredTrait
		=> SpontaneousManager.SpontaneousTriggeredTrait;

	public CardAction MakeScryAction(int amount)
		=> new ScryAction { Amount = amount };

	public CardAction MakeOnDiscardAction(CardAction action)
		=> new OnDiscardManager.TriggerAction { Action = action };

	public CardAction MakeOnTurnEndAction(CardAction action)
		=> new OnTurnEndManager.TriggerAction { Action = action };

	public CardAction MakeSpontaneousAction(CardAction action)
		=> new SpontaneousManager.TriggerAction { Action = action };

	public void RegisterHook(IBlochHook hook, double priority)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IBlochHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}
