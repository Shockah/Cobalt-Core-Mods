using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ILimitedApi"/>
		ILimitedApi Limited { get; }

		/// <summary>
		/// Allows working with and using the Limited card trait.
		/// Limited cards can be used multiple times each combat before getting exhausted.
		/// </summary>
		public interface ILimitedApi
		{
			/// <summary>
			/// The Limited card trait.
			/// </summary>
			/// <remarks>
			/// A limited card by default exhausts after being played twice.
			/// This can be changed via <see cref="SetBaseLimitedUses(string,int)"/> or <see cref="SetBaseLimitedUses(string,Upgrade,int)"/>.
			/// </remarks>
			ICardTraitEntry Trait { get; }
			
			/// <summary>
			/// The default amount of times a Limited card can be played before it exhausts, if not overridden in any way.
			/// Defaults to 2.
			/// </summary>
			int DefaultLimitedUses { get; }
			
			/// <summary>
			/// Returns the amount of times a card with the given key can be played before it exhausts.
			/// </summary>
			/// <param name="key">The <see cref="Card.Key">key</see> of the card.</param>
			/// <param name="upgrade">The specific upgrade to check.</param>
			/// <returns>The amount of times the card with the given key can be played before it exhausts.</returns>
			int GetBaseLimitedUses(string key, Upgrade upgrade);
			
			/// <summary>
			/// Sets the amount of times a card with the given key can be played before it exhausts.
			/// </summary>
			/// <remarks>
			/// This method sets the value for all possible upgrades of the card.
			/// To set the value for a specific upgrade, use <see cref="SetBaseLimitedUses(string,Upgrade,int)"/> instead.
			/// </remarks>
			/// <param name="key">The <see cref="Card.Key">key</see> of the card.</param>
			/// <param name="value">The amount of times the card with the given key can be played before it exhausts.</param>
			void SetBaseLimitedUses(string key, int value);
			
			/// <summary>
			/// Sets the amount of times a card with the given key can be played before it exhausts.
			/// </summary>
			/// <param name="key">The <see cref="Card.Key">key</see> of the card.</param>
			/// <param name="upgrade">The specific upgrade to set the amount for.</param>
			/// <param name="value">The amount of times the card with the given key can be played before it exhausts.</param>
			void SetBaseLimitedUses(string key, Upgrade upgrade, int value);
			
			/// <summary>
			/// Returns the amount of times the given card can be played each combat before it exhausts, taking into account any modifiers.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			/// <returns>The amount of times the given card can be played each combat before it exhausts, taking into account any modifiers.</returns>
			int GetStartingLimitedUses(State state, Card card);
			
			/// <summary>
			/// Returns the amount of times the given card can still be played this combat before it exhausts.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			/// <returns>The amount of times the given card can still be played this combat before it exhausts.</returns>
			int GetLimitedUses(State state, Card card);
			
			/// <summary>
			/// Sets the amount of times the given card can still be played this combat before it exhausts.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			/// <param name="value">The amount of times the given card can still be played this combat before it exhausts.</param>
			void SetLimitedUses(State state, Card card, int value);
			
			/// <summary>
			/// Resets the amount of times the given card can still be played this combat to the <see cref="GetStartingLimitedUses">starting amount</see>.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			void ResetLimitedUses(State state, Card card);
			
			/// <summary>
			/// Casts the action to <see cref="IVariableHint"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IVariableHint"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IVariableHint? AsVariableHint(AVariableHint action);
			
			/// <summary>
			/// Creates a new variable hint action for the amount of times the given card can still be played this combat.
			/// </summary>
			/// <param name="cardId">The ID of the card to check the amount of times it can still be played this combat.</param>
			/// <returns>The new variable hint action.</returns>
			IVariableHint MakeVariableHint(int cardId);
			
			/// <summary>
			/// Casts the action to <see cref="IChangeLimitedUsesAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IChangeLimitedUsesAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IChangeLimitedUsesAction? AsChangeLimitedUsesAction(CardAction action);
			
			/// <summary>
			/// Creates a new action which changes the amount of times the given card can still be played this combat.
			/// </summary>
			/// <param name="cardId">The ID of the card to set the amount of times it can still be played this combat.</param>
			/// <param name="amount">A modifier amount. See <see cref="AStatus.statusAmount"/>.</param>
			/// <param name="mode">A modifier mode. See <see cref="AStatus.mode"/>.</param>
			/// <returns>A new action which changes the amount of times the given card can still be played this combat.</returns>
			IChangeLimitedUsesAction MakeChangeLimitedUsesAction(int cardId, int amount, AStatusMode mode = AStatusMode.Add);

			/// <summary>
			/// Allows modifying an <see cref="ACardSelect"/> action with Limited-related changes.
			/// </summary>
			/// <param name="action">The action to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardSelect ModifyCardSelect(ACardSelect action);
			
			/// <summary>
			/// Allows modifying a <see cref="CardBrowse"/> route with Limited-related changes.
			/// </summary>
			/// <param name="route">The route to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardBrowse ModifyCardBrowse(CardBrowse route);

			/// <summary>
			/// Retrieves an icon for the Limited card trait with the given amount of uses.
			/// Amounts of 10+ get replaced with a <c>+</c> symbol.
			/// </summary>
			/// <param name="amount">The amount.</param>
			/// <returns>The icon.</returns>
			Spr GetIcon(int amount);

			/// <summary>
			/// Retrieves the top icon layer for the Limited card trait with the given amount of uses.
			/// Amounts of 10+ get replaced with a <c>+</c> symbol.
			/// </summary>
			/// <param name="amount">The amount.</param>
			/// <returns>The icon.</returns>
			Spr GetTopIconLayer(int amount);
			
			/// <summary>
			/// Registers a new hook related to the Limited card trait.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to the Limited card trait.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// A variable hint action for the amount of times a given card can still be played this combat.
			/// </summary>
			public interface IVariableHint : ICardAction<AVariableHint>
			{
				/// <summary>
				/// The ID of the card to check the amount of times it can still be played this combat.
				/// </summary>
				int CardId { get; set; }

				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IVariableHint SetCardId(int value);
			}
			
			/// <summary>
			/// An action which changes the amount of times the given card can still be played this combat.
			/// </summary>
			public interface IChangeLimitedUsesAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The ID of the card to set the amount of times it can still be played this combat.
				/// </summary>
				int CardId { get; set; }
				
				/// <summary>
				/// A modifier amount. See <see cref="AStatus.statusAmount"/>.
				/// </summary>
				int Amount { get; set; }
				
				/// <summary>
				/// A modifier mode. See <see cref="AStatus.mode"/>.
				/// </summary>
				AStatusMode Mode { get; set; }

				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IChangeLimitedUsesAction SetCardId(int value);
				
				/// <summary>
				/// Sets <see cref="Amount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IChangeLimitedUsesAction SetAmount(int value);
				
				/// <summary>
				/// Sets <see cref="Mode"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IChangeLimitedUsesAction SetMode(AStatusMode value);
			}
			
			/// <summary>
			/// An <see cref="ACardSelect"/> action wrapper, which allows modifying it with Limited-related changes.
			/// </summary>
			public interface ICardSelect : ICardAction<ACardSelect>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Limited trait.
				/// </summary>
				bool? FilterLimited { get; set; }

				/// <summary>
				/// Sets <see cref="FilterLimited"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardSelect SetFilterLimited(bool? value);
			}
			
			/// <summary>
			/// A <see cref="CardBrowse"/> route wrapper, which allows modifying it with Limited-related changes.
			/// </summary>
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Limited trait.
				/// </summary>
				bool? FilterLimited { get; set; }

				/// <summary>
				/// Sets <see cref="FilterLimited"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardBrowse SetFilterLimited(bool? value);
			}

			/// <summary>
			/// A hook related to the Limited card trait.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Modifies the amount of times the given card can be played each combat before it exhausts.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the event is considered handled and no further hooks should be called; <c>false</c> otherwise.</returns>
				bool ModifyLimitedUses(IModifyLimitedUsesArgs args) => false;
				
				/// <summary>
				/// Controls whether a Limited card should be permanently removed instead of being exhausted.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the card should be permanently removed; <c>false</c> if the card should be exhausted; <c>null</c> if this hook does not care.</returns>
				bool? IsSingleUseLimited(IIsSingleUseLimitedArgs args) => null;

				/// <summary>
				/// The arguments for the <see cref="ModifyLimitedUses"/> hook method.
				/// </summary>
				public interface IModifyLimitedUsesArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The Limited card.
					/// </summary>
					Card Card { get; }
					
					/// <summary>
					/// The <see cref="ILimitedApi.GetBaseLimitedUses">base uses</see> of the card.
					/// </summary>
					int BaseUses { get; }
					
					/// <summary>
					/// The (potentially) modified amount of times the given card can be played each combat before it exhausts.
					/// </summary>
					/// <remarks>
					/// This value can be freely changed by a hook.
					/// </remarks>
					int Uses { get; set; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="IsSingleUseLimited"/> hook method.
				/// </summary>
				public interface IIsSingleUseLimitedArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The Limited card.
					/// </summary>
					Card Card { get; }
				}
			}
		}
	}
}
