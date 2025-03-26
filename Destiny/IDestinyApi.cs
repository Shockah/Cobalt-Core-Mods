using System.Collections.Generic;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Destiny;

public interface IDestinyApi
{
	IDeckEntry DestinyDeck { get; }
	
	IStatusEntry MagicFindStatus { get; }
	IStatusEntry PristineShieldStatus { get; }
	
	ICardTraitEntry EnchantedTrait { get; }
	ICardTraitEntry ExplosiveTrait { get; }

	Spr? GetEnchantedCardArt(Card? card, Spr? defaultArt = null, int[]? split = null);
	int GetMaxEnchantLevel(string cardKey, Upgrade upgrade);
	int GetEnchantLevel(Card card);
	void SetEnchantLevel(Card card, int level);
	IKokoroApi.IV2.IActionCostsApi.ICost? GetNextEnchantLevelCost(Card card);
	IKokoroApi.IV2.IActionCostsApi.ICost? GetEnchantLevelCost(string cardKey, Upgrade upgrade, int level);
	void SetEnchantLevelCost(string cardKey, int level, IKokoroApi.IV2.IActionCostsApi.ICost? cost);
	void SetEnchantLevelCost(string cardKey, Upgrade upgrade, int level, IKokoroApi.IV2.IActionCostsApi.ICost? cost);
	IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>? GetEnchantLevelPayment(Card card, int level);
	void SetEnchantLevelPayment(Card card, int level, IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult> payment);
	void ClearEnchantLevelPayments(Card card);
	bool TryEnchant(State state, Card card, bool fromUserInteraction = true);
	
	IEnchantGateAction MakeEnchantGateAction(int level);
	IEnchantGateAction? AsEnchantGateAction(CardAction action);
	IEnchantedAction MakeEnchantedAction(int cardId, int level, CardAction action);
	IEnchantedAction? AsEnchantedAction(CardAction action);
	
	IImbueAction? AsImbueAction(CardAction action);
	IImbueTraitAction MakeImbueTraitAction(int level, ICardTraitEntry trait);
	IImbueTraitAction? AsImbueTraitAction(CardAction action);
	IImbueDiscountAction MakeImbueDiscountAction(int level, int discount = -1);
	IImbueDiscountAction? AsImbueDiscountAction(CardAction action);
	
	void RegisterHook(IHook hook, double priority = 0);
	void UnregisterHook(IHook hook);

	public interface IEnchantGateAction : IKokoroApi.IV2.ICardAction<CardAction>
	{
		int Level { get; set; }
		
		IEnchantGateAction SetLevel(int value);
	}

	public interface IEnchantedAction : IKokoroApi.IV2.ICardAction<CardAction>
	{
		int CardId { get; set; }
		int Level { get; set; }
		CardAction Action { get; set; }
		
		IEnchantedAction SetCardId(int value);
		IEnchantedAction SetLevel(int value);
		IEnchantedAction SetAction(CardAction value);
	}
	
	public interface IImbueAction : IKokoroApi.IV2.ICardAction<CardAction>
	{
		int Level { get; set; }
		
		IImbueAction SetLevel(int value);
	
		void ImbueCard(State state, Card card);
	}

	public interface IImbueTraitAction : IImbueAction
	{
		ICardTraitEntry? Trait { get; set; }
		
		IImbueTraitAction SetTrait(ICardTraitEntry value);
	}

	public interface IImbueDiscountAction : IImbueAction
	{
		int Discount { get; set; }
		
		IImbueDiscountAction SetDiscount(int value);
	}
	
	public interface IHook
	{
		void ModifyExplosiveDamage(IModifyExplosiveDamageArgs args) { }
		void OnExplosiveTrigger(IOnExplosiveTriggerArgs args) { }
		bool? OnEnchant(IOnEnchantArgs args) => null;
		void AfterEnchant(IAfterEnchantArgs args) { }
		void OnPristineShieldTrigger(IOnPristineShieldTriggerArgs args) { }

		public interface IModifyExplosiveDamageArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card? Card { get; }
			int BaseDamage { get; }
			int CurrentDamage { get; set; }
		}
		
		public interface IOnExplosiveTriggerArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card? Card { get; }
			CardAction? Action { get; set; }
		}

		public interface IOnEnchantArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card Card { get; }
			bool FromUserInteraction { get; }
			int EnchantLevel { get; }
			int MaxEnchantLevel { get; }
		}

		public interface IAfterEnchantArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card Card { get; }
			bool FromUserInteraction { get; }
			int EnchantLevel { get; }
			int MaxEnchantLevel { get; }
		}
		
		public interface IOnPristineShieldTriggerArgs
		{
			State State { get; }
			Combat Combat { get; }
			Ship Ship { get; }
			int Damage { get; }
			bool TickDown { get; set; }
		}
	}
}