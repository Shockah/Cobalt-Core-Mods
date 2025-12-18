using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IFiniteApi"/>
		IFiniteApi Finite { get; }

		/// <summary>
		/// Allows working with and using the Finite card trait.
		/// Finite cards can be used multiple times each turn before getting discarded.
		/// </summary>
		public interface IFiniteApi
		{
			/// <summary>
			/// The Finite card trait.
			/// </summary>
			/// <remarks>
			/// A finite card by default discards after being played twice.
			/// This can be changed via <see cref="SetBaseFiniteUses(string,int)"/> or <see cref="SetBaseFiniteUses(string,Upgrade,int)"/>.
			/// </remarks>
			ICardTraitEntry Trait { get; }
			
			/// <summary>
			/// The default amount of times a Finite card can be played before it discards, if not overridden in any way.
			/// Defaults to 3.
			/// </summary>
			int DefaultFiniteUses { get; }
			
			/// <summary>
			/// Returns the amount of times a card with the given key can be played before it discards.
			/// </summary>
			/// <param name="key">The <see cref="Card.Key">key</see> of the card.</param>
			/// <param name="upgrade">The specific upgrade to check.</param>
			/// <returns>The amount of times the card with the given key can be played before it discards.</returns>
			int GetBaseFiniteUses(string key, Upgrade upgrade);
			
			/// <summary>
			/// Sets the amount of times a card with the given key can be played before it discards.
			/// </summary>
			/// <remarks>
			/// This method sets the value for all possible upgrades of the card.
			/// To set the value for a specific upgrade, use <see cref="SetBaseFiniteUses(string,Upgrade,int)"/> instead.
			/// </remarks>
			/// <param name="key">The <see cref="Card.Key">key</see> of the card.</param>
			/// <param name="value">The amount of times the card with the given key can be played before it discards.</param>
			void SetBaseFiniteUses(string key, int value);
			
			/// <summary>
			/// Sets the amount of times a card with the given key can be played before it discards.
			/// </summary>
			/// <param name="key">The <see cref="Card.Key">key</see> of the card.</param>
			/// <param name="upgrade">The specific upgrade to set the amount for.</param>
			/// <param name="value">The amount of times the card with the given key can be played before it discards.</param>
			void SetBaseFiniteUses(string key, Upgrade upgrade, int value);
			
			/// <summary>
			/// Returns the amount of times the given card can be played each turn before it discards, taking into account any modifiers.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			/// <returns>The amount of times the given card can be played each turn before it discards, taking into account any modifiers.</returns>
			int GetStartingFiniteUses(State state, Card card);
			
			/// <summary>
			/// Returns the amount of times the given card can still be played this turn before it discards.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			/// <returns>The amount of times the given card can still be played this turn before it discards.</returns>
			int GetFiniteUses(State state, Card card);
			
			/// <summary>
			/// Sets the amount of times the given card can still be played this turn before it discards.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			/// <param name="value">The amount of times the given card can still be played this turn before it discards.</param>
			void SetFiniteUses(State state, Card card, int value);
			
			/// <summary>
			/// Resets the amount of times the given card can still be played this turn to the <see cref="GetStartingFiniteUses">starting amount</see>.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The card.</param>
			void ResetFiniteUses(State state, Card card);
			
			/// <summary>
			/// Casts the action to <see cref="IVariableHint"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IVariableHint"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IVariableHint? AsVariableHint(AVariableHint action);
			
			/// <summary>
			/// Creates a new variable hint action for the amount of times the given card can still be played this turn.
			/// </summary>
			/// <param name="cardId">The ID of the card to check the amount of times it can still be played this turn.</param>
			/// <returns>The new variable hint action.</returns>
			IVariableHint MakeVariableHint(int cardId);
			
			/// <summary>
			/// Casts the action to <see cref="IChangeFiniteUsesAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IChangeFiniteUsesAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IChangeFiniteUsesAction? AsChangeFiniteUsesAction(CardAction action);
			
			/// <summary>
			/// Creates a new action which changes the amount of times the given card can still be played this turn.
			/// </summary>
			/// <param name="cardId">The ID of the card to set the amount of times it can still be played this turn.</param>
			/// <param name="amount">A modifier amount. See <see cref="AStatus.statusAmount"/>.</param>
			/// <param name="mode">A modifier mode. See <see cref="AStatus.mode"/>.</param>
			/// <returns>A new action which changes the amount of times the given card can still be played this turn.</returns>
			IChangeFiniteUsesAction MakeChangeFiniteUsesAction(int cardId, int amount, AStatusMode mode = AStatusMode.Add);

			/// <summary>
			/// Allows modifying an <see cref="ACardSelect"/> action with Finite-related changes.
			/// </summary>
			/// <param name="action">The action to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardSelect ModifyCardSelect(ACardSelect action);
			
			/// <summary>
			/// Allows modifying a <see cref="CardBrowse"/> route with Finite-related changes.
			/// </summary>
			/// <param name="route">The route to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardBrowse ModifyCardBrowse(CardBrowse route);

			/// <summary>
			/// Retrieves an icon for the Finite card trait with the given amount of uses.
			/// Amounts of 10+ get replaced with a <c>+</c> symbol.
			/// </summary>
			/// <param name="amount">The amount.</param>
			/// <returns>The icon.</returns>
			Spr GetIcon(int amount);
			
			/// <summary>
			/// Registers a new hook related to the Finite card trait.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to the Finite card trait.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// A variable hint action for the amount of times a given card can still be played this turn.
			/// </summary>
			public interface IVariableHint : ICardAction<AVariableHint>
			{
				/// <summary>
				/// The ID of the card to check the amount of times it can still be played this turn.
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
			/// An action which changes the amount of times the given card can still be played this turn.
			/// </summary>
			public interface IChangeFiniteUsesAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The ID of the card to set the amount of times it can still be played this turn.
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
				IChangeFiniteUsesAction SetCardId(int value);
				
				/// <summary>
				/// Sets <see cref="Amount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IChangeFiniteUsesAction SetAmount(int value);
				
				/// <summary>
				/// Sets <see cref="Mode"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IChangeFiniteUsesAction SetMode(AStatusMode value);
			}
			
			/// <summary>
			/// An <see cref="ACardSelect"/> action wrapper, which allows modifying it with Finite-related changes.
			/// </summary>
			public interface ICardSelect : ICardAction<ACardSelect>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Finite trait.
				/// </summary>
				bool? FilterFinite { get; set; }

				/// <summary>
				/// Sets <see cref="FilterFinite"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardSelect SetFilterFinite(bool? value);
			}
			
			/// <summary>
			/// A <see cref="CardBrowse"/> route wrapper, which allows modifying it with Finite-related changes.
			/// </summary>
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Finite trait.
				/// </summary>
				bool? FilterFinite { get; set; }

				/// <summary>
				/// Sets <see cref="FilterFinite"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardBrowse SetFilterFinite(bool? value);
			}

			/// <summary>
			/// A hook related to the Finite card trait.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Modifies the amount of times the given card can be played each turn before it discards.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the event is considered handled and no further hooks should be called; <c>false</c> otherwise.</returns>
				bool ModifyFiniteUses(IModifyFiniteUsesArgs args) => false;

				/// <summary>
				/// The arguments for the <see cref="ModifyFiniteUses"/> hook method.
				/// </summary>
				public interface IModifyFiniteUsesArgs
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
					/// The <see cref="IFiniteApi.GetBaseFiniteUses">base uses</see> of the card.
					/// </summary>
					int BaseUses { get; }
					
					/// <summary>
					/// The (potentially) modified amount of times the given card can be played each turn before it discards.
					/// </summary>
					/// <remarks>
					/// This value can be freely changed by a hook.
					/// </remarks>
					int Uses { get; set; }
				}
			}
		}
	}
}
