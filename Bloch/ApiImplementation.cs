using Nickel;
using System.Collections.Generic;

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
		=> IntuitionManager.IntuitionStatus;

	public IStatusEntry MindMapStatus
		=> MindMapManager.MindMapStatus;

	public IStatusEntry PersonalitySplitStatus
		=> SplitPersonalityManager.SplitPersonalityStatus;

	public ICardTraitEntry SpontaneousTriggeredTrait
		=> SpontaneousManager.SpontaneousTriggeredTrait;

	public CardAction MakeChooseAura(Card card, int amount, string? uiSubtitle = null, int actionId = 0)
	{
		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<Dictionary<int, Status>>(card, "ChosenAuras").TryGetValue(actionId, out var chosenAura))
			return new AStatus
			{
				targetPlayer = true,
				status = chosenAura,
				statusAmount = amount
			};
		else
			return new AuraManager.ChooseAuraAction { CardId = card.uuid, ActionId = actionId, Amount = amount, UISubtitle = uiSubtitle ?? GetChooseAuraOnPlayUISubtitle(amount) };
	}

	public CardAction MakeScryAction(int amount)
		=> new ScryAction { Amount = amount };

	public CardAction MakeOnDiscardAction(CardAction action)
		=> new OnDiscardManager.TriggerAction { Action = action };

	public CardAction MakeOnTurnEndAction(CardAction action)
		=> new OnTurnEndManager.TriggerAction { Action = action };

	public CardAction MakeSpontaneousAction(CardAction action)
		=> new SpontaneousManager.TriggerAction { Action = action };

	public string GetChooseAuraOnPlayUISubtitle(int amount)
		=> ModEntry.Instance.Localizations.Localize(["action", "ChooseAura", "uiSubtitle", "OnPlay"]);

	public string GetChooseAuraOnDiscardUISubtitle(int amount)
		=> ModEntry.Instance.Localizations.Localize(["action", "ChooseAura", "uiSubtitle", "OnDiscard"]);

	public string GetChooseAuraOnTurnEndUISubtitle(int amount)
		=> ModEntry.Instance.Localizations.Localize(["action", "ChooseAura", "uiSubtitle", "OnTurnEnd"]);

	public void RegisterHook(IBlochHook hook, double priority)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IBlochHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}
