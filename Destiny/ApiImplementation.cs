using System.Collections.Generic;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Destiny;

public sealed class ApiImplementation : IDestinyApi
{
	public IDeckEntry DestinyDeck
		=> ModEntry.Instance.DestinyDeck;

	public IStatusEntry MagicFindStatus
		=> MagicFind.MagicFindStatus;

	public IStatusEntry PristineShieldStatus
		=> PristineShield.PristineShieldStatus;

	public ICardTraitEntry EnchantedTrait
		=> Enchanted.EnchantedTrait;
	
	public ICardTraitEntry ExplosiveTrait
		=> Explosive.ExplosiveTrait;

	public Spr? GetEnchantedCardArt(Card? card, Spr? defaultArt = null, int[]? split = null)
		=> Enchanted.GetCardArt(card, defaultArt, split);

	public int GetMaxEnchantLevel(string cardKey, Upgrade upgrade)
		=> Enchanted.GetMaxEnchantLevel(cardKey, upgrade);

	public int GetEnchantLevel(Card card)
		=> Enchanted.GetEnchantLevel(card);

	public void SetEnchantLevel(Card card, int level)
		=> Enchanted.SetEnchantLevel(card, level);

	public IKokoroApi.IV2.IActionCostsApi.ICost? GetNextEnchantLevelCost(Card card)
		=> Enchanted.GetNextEnchantLevelCost(card);

	public IKokoroApi.IV2.IActionCostsApi.ICost? GetEnchantLevelCost(string cardKey, Upgrade upgrade, int level)
		=> Enchanted.GetEnchantLevelCost(cardKey, upgrade, level);

	public void SetEnchantLevelCost(string cardKey, int level, IKokoroApi.IV2.IActionCostsApi.ICost? cost)
		=> Enchanted.SetEnchantLevelCost(cardKey, level, cost);

	public void SetEnchantLevelCost(string cardKey, Upgrade upgrade, int level, IKokoroApi.IV2.IActionCostsApi.ICost? cost)
		=> Enchanted.SetEnchantLevelCost(cardKey, upgrade, level, cost);

	public IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>? GetEnchantLevelPayment(Card card, int level)
		=> Enchanted.GetEnchantLevelPayment(card, level);

	public void SetEnchantLevelPayment(Card card, int level, IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult> payment)
		=> Enchanted.SetEnchantLevelPayment(card, level, payment);

	public void ClearEnchantLevelPayments(Card card)
		=> Enchanted.ClearEnchantLevelPayments(card);

	public bool TryEnchant(State state, Card card, bool fromUserInteraction = true)
		=> Enchanted.TryEnchant(state, card, fromUserInteraction);

	public IDestinyApi.IEnchantGateAction MakeEnchantGateAction(int level)
		=> new EnchantGateAction { Level = level };

	public IDestinyApi.IEnchantGateAction? AsEnchantGateAction(CardAction action)
		=> action as IDestinyApi.IEnchantGateAction;

	public IDestinyApi.IEnchantedAction MakeEnchantedAction(int cardId, int level, CardAction action)
		=> new EnchantedAction { CardId = cardId, Level = level, Action = action };

	public IDestinyApi.IEnchantedAction? AsEnchantedAction(CardAction action)
		=> action as IDestinyApi.IEnchantedAction;

	public IDestinyApi.IImbueAction? AsImbueAction(CardAction action)
		=> action as IDestinyApi.IImbueAction;

	public IDestinyApi.IImbueTraitAction MakeImbueTraitAction(int level, ICardTraitEntry trait)
		=> new ImbueTraitAction { Level = level, Trait = trait };

	public IDestinyApi.IImbueTraitAction? AsImbueTraitAction(CardAction action)
		=> action as IDestinyApi.IImbueTraitAction;

	public IDestinyApi.IImbueDiscountAction MakeImbueDiscountAction(int level, int discount = -1)
		=> new ImbueDiscountAction { Level = level, Discount = discount };

	public IDestinyApi.IImbueDiscountAction? AsImbueDiscountAction(CardAction action)
		=> action as IDestinyApi.IImbueDiscountAction;

	public void RegisterHook(IDestinyApi.IHook hook, double priority = 0)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IDestinyApi.IHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}