using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IEvadeHookApi"/>
		IEvadeHookApi EvadeHook { get; }

		/// <summary>
		/// Allows modifying <see cref="Status.evade"/> behavior.
		/// </summary>
		public interface IEvadeHookApi
		{
			/// <summary>
			/// The default evade action.
			/// </summary>
			IEvadeActionEntry DefaultAction { get; }
			
			/// <summary>
			/// The default evade payment option, as in, paying with one <see cref="Status.evade"/>.
			/// </summary>
			IEvadePaymentOption DefaultActionPaymentOption { get; }
			
			/// <summary>
			/// The debug evade payment option, as in, for free, when Debug mode is enabled and the Shift key is held.
			/// </summary>
			IEvadePaymentOption DebugActionPaymentOption { get; }
			
			/// <summary>
			/// The evade precondition that disallows movement if the player has an <see cref="TrashAnchor">Anchor card</see> in hand. 
			/// </summary>
			IEvadePrecondition DefaultActionAnchorPrecondition { get; }
			
			/// <summary>
			/// The evade precondition that disallows movement if the player's ship has any <see cref="Status.lockdown">Engine Lock</see>.
			/// </summary>
			IEvadePrecondition DefaultActionEngineLockPrecondition { get; }
			
			/// <summary>
			/// The evade postcondition that disallows movement if the player's ship has any <see cref="Status.engineStall">Engine Stall</see>.
			/// </summary>
			IEvadePostcondition DefaultActionEngineStallPostcondition { get; }
			
			/// <summary>
			/// Registers a new type of action triggered by the on-screen evade buttons, keybinds or controller buttons.
			/// </summary>
			/// <param name="action">The new action.</param>
			/// <param name="priority">The priority for the action. Higher priority actions are called before lower priority ones. Defaults to <c>0</c>.</param>
			/// <returns>An entry representing the new action and allowing further modifications.</returns>
			IEvadeActionEntry RegisterAction(IEvadeAction action, double priority = 0);

			/// <summary>
			/// Creates a new result of a precondition.
			/// </summary>
			/// <param name="isAllowed">Whether the action is allowed.</param>
			/// <returns>The new result.</returns>
			IEvadePrecondition.IResult MakePreconditionResult(bool isAllowed);
			
			/// <summary>
			/// Creates a new result of a postcondition.
			/// </summary>
			/// <param name="isAllowed">Whether the action is allowed.</param>
			/// <returns>The new result.</returns>
			IEvadePostcondition.IResult MakePostconditionResult(bool isAllowed);

			/// <summary>
			/// Returns the next action entry that would be ran if the given evade action was requested.
			/// </summary>
			/// <remarks>
			/// This method (along with <see cref="IHook.ShouldShowEvadeButton"/>, <see cref="DidHoverButton"/> and <see cref="RunNextAction"/>) can be used to implement custom evade buttons.
			/// </remarks>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="direction">The direction of movement.</param>
			/// <returns>The action entry that would be ran.</returns>
			[Obsolete("Use the `(State state, Combat combat, Direction direction, bool forRendering)` overload instead.")]
			IEvadeActionEntry? GetNextAction(State state, Combat combat, Direction direction);

			/// <summary>
			/// Returns the next action entry that would be ran if the given evade action was requested.
			/// </summary>
			/// <remarks>
			/// This method (along with <see cref="IHook.ShouldShowEvadeButton"/>, <see cref="DidHoverButton"/> and <see cref="RunNextAction"/>) can be used to implement custom evade buttons.
			/// </remarks>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="direction">The direction of movement.</param>
			/// <param name="forRendering">Whether this method was called for rendering purposes, or actual action purposes otherwise.</param>
			/// <returns>The action entry that would be ran.</returns>
			IEvadeActionEntry? GetNextAction(State state, Combat combat, Direction direction, bool forRendering);
			
			/// <summary>
			/// Raises the events related to hovering over an evade button.
			/// </summary>
			/// <remarks>
			/// This method (along with <see cref="IHook.ShouldShowEvadeButton"/>, <see cref="GetNextAction(State,Combat,Shockah.Kokoro.IKokoroApi.IV2.IEvadeHookApi.Direction,bool)"/> and <see cref="RunNextAction"/>) can be used to implement custom evade buttons.
			/// </remarks>
			/// <seealso cref="IEvadeAction.EvadeButtonHovered">IEvadeAction.EvadeButtonHovered</seealso>
			/// <seealso cref="IEvadePaymentOption.EvadeButtonHovered">IEvadePaymentOption.EvadeButtonHovered</seealso>
			/// <seealso cref="IEvadePrecondition.EvadeButtonHovered">IEvadePrecondition.EvadeButtonHovered</seealso>
			/// <seealso cref="IEvadePostcondition.EvadeButtonHovered">IEvadePostcondition.EvadeButtonHovered</seealso>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="direction">The direction of movement.</param>
			void DidHoverButton(State state, Combat combat, Direction direction);
			
			/// <summary>
			/// Runs the next action entry for the given direction.
			/// </summary>
			/// <remarks>
			/// This method (along with <see cref="IHook.ShouldShowEvadeButton"/>, <see cref="GetNextAction(State,Combat,Shockah.Kokoro.IKokoroApi.IV2.IEvadeHookApi.Direction,bool)"/> and <see cref="DidHoverButton"/>) can be used to implement custom evade buttons.
			/// </remarks>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="direction">The direction of movement.</param>
			/// <returns>The result of the action.</returns>
			IRunActionResult RunNextAction(State state, Combat combat, Direction direction);
			
			/// <summary>
			/// Registers a new hook related to <see cref="Status.evade"/>.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to <see cref="Status.evade"/>.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// Describes the movement direction.
			/// </summary>
			/// <remarks>
			/// Can be casted to <see cref="int"/> for the X axis offset.
			/// </remarks>
			public enum Direction
			{
				Left = -1,
				Right = 1
			}

			/// <summary>
			/// Describes the result of a single evade action execution.
			/// </summary>
			public interface IRunActionResult
			{
				/// <summary>
				/// The game state.
				/// </summary>
				State State { get; }
					
				/// <summary>
				/// The current combat.
				/// </summary>
				Combat Combat { get; }
					
				/// <summary>
				/// The direction of movement.
				/// </summary>
				Direction Direction { get; }

				/// <summary>
				/// The entry of the action that was being ran, if any.
				/// </summary>
				IEvadeActionEntry? Entry { get; }

				/// <summary>
				/// The payment option that was used to pay for this action, if any action was taken.
				/// </summary>
				IEvadePaymentOption? PaymentOption { get; }
				
				/// <summary>
				/// The precondition failure hook arguments, if the action failed due to one.
				/// </summary>
				IHook.IEvadePreconditionFailedArgs? PreconditionFailed { get; }
				
				/// <summary>
				/// The postcondition failure hook arguments, if the action failed due to one.
				/// </summary>
				IHook.IEvadePostconditionFailedArgs? PostconditionFailed { get; }
				
				/// <summary>
				/// The after evade hook arguments, if the action succeeded.
				/// </summary>
				IHook.IAfterEvadeArgs? Success { get; }
			}

			/// <summary>
			/// Represents an entry for a type of action triggered by the on-screen evade buttons, keybinds or controller buttons, and allowing further modifications.
			/// </summary>
			public interface IEvadeActionEntry
			{
				/// <summary>
				/// The type of action triggered by the on-screen evade buttons, keybinds or controller buttons.
				/// </summary>
				IEvadeAction Action { get; }
				
				/// <summary>
				/// A sorted enumerable over all of the payment options for this action.
				/// </summary>
				IEnumerable<IEvadePaymentOption> PaymentOptions { get; }
				
				/// <summary>
				/// A sorted enumerable over all of the preconditions that need to be satisfied before this action can take place.
				/// </summary>
				IEnumerable<IEvadePrecondition> Preconditions { get; }
				
				/// <summary>
				/// A sorted enumerable over all of the postconditions that need to be satisfied before this action can take place, but after it is paid for.
				/// </summary>
				IEnumerable<IEvadePostcondition> Postconditions { get; }

				/// <summary>
				/// Registers a new payment option.
				/// </summary>
				/// <param name="paymentOption">The new payment option.</param>
				/// <param name="priority">The priority for the payment option. Higher priority options are called before lower priority ones. Defaults to <c>0</c>.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry RegisterPaymentOption(IEvadePaymentOption paymentOption, double priority = 0);
				
				/// <summary>
				/// Unregisters a payment option.
				/// </summary>
				/// <param name="paymentOption">The payment option.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry UnregisterPaymentOption(IEvadePaymentOption paymentOption);

				/// <summary>
				/// Registers a new precondition that needs to be satisfied before this action can take place.
				/// </summary>
				/// <param name="precondition">The new precondition.</param>
				/// <param name="priority">The priority for the precondition. Higher priority preconditions are called before lower priority ones. Defaults to <c>0</c>.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry RegisterPrecondition(IEvadePrecondition precondition, double priority = 0);
				
				/// <summary>
				/// Unregisters a precondition.
				/// </summary>
				/// <param name="precondition">The precondition.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry UnregisterPrecondition(IEvadePrecondition precondition);

				/// <summary>
				/// Sets this entry to dynamically inherit all preconditions from another entry.
				/// </summary>
				/// <param name="entry">The entry to inherit from.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry InheritPreconditions(IEvadeActionEntry entry);

				/// <summary>
				/// Registers a new postcondition that needs to be satisfied before this action can take place, but after it is paid for.
				/// </summary>
				/// <param name="postcondition">The new precondition.</param>
				/// <param name="priority">The priority for the postcondition. Higher priority postconditions are called before lower priority ones. Defaults to <c>0</c>.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry RegisterPostcondition(IEvadePostcondition postcondition, double priority = 0);
				
				/// <summary>
				/// Unregisters a postcondition.
				/// </summary>
				/// <param name="postcondition">The postcondition.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry UnregisterPostcondition(IEvadePostcondition postcondition);
				
				/// <summary>
				/// Sets this entry to dynamically inherit all postconditions from another entry.
				/// </summary>
				/// <param name="entry">The entry to inherit from.</param>
				/// <returns>This object after the change.</returns>
				IEvadeActionEntry InheritPostconditions(IEvadeActionEntry entry);
			}
			
			/// <summary>
			/// Represents a type of action triggered by the on-screen evade buttons, keybinds or controller buttons.
			/// </summary>
			public interface IEvadeAction
			{
				/// <summary>
				/// Tests if this action can currently be used.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>Whether this action can currently be used.</returns>
				bool CanDoEvadeAction(ICanDoEvadeArgs args);
				
				/// <summary>
				/// Provides a list of actions to queue related to a single action trigger.
				/// </summary>
				/// <remarks>
				/// It is allowed for this method to execute non-action code and return an empty action list.
				/// </remarks>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>The list of actions to queue.</returns>
				IReadOnlyList<CardAction> ProvideEvadeActions(IProvideEvadeActionsArgs args);
				
				/// <summary>
				/// Ran when an on-screen evade button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void EvadeButtonHovered(IEvadeButtonHoveredArgs args) { }

				/// <summary>
				/// The arguments for the <see cref="CanDoEvadeAction"/> method.
				/// </summary>
				public interface ICanDoEvadeArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="ProvideEvadeActions"/> method.
				/// </summary>
				public interface IProvideEvadeActionsArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The payment option that was used to pay for this action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="EvadeButtonHovered"/> method.
				/// </summary>
				public interface IEvadeButtonHoveredArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
				}
			}

			/// <summary>
			/// Represents a way of paying for an action triggered by the on-screen evade buttons, keybinds or controller buttons.
			/// </summary>
			/// <remarks>
			/// An action requires at least one payment option, or otherwise it cannot be triggered.
			/// </remarks>
			public interface IEvadePaymentOption
			{
				/// <summary>
				/// Tests if this payment option can currently be used.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>Whether this payment option can currently be used.</returns>
				bool CanPayForEvade(ICanPayForEvadeArgs args);
				
				/// <summary>
				/// Provides a list of actions to queue related to a single payment.
				/// </summary>
				/// <remarks>
				/// It is allowed for this method to execute non-action payments and return an empty action list.
				/// </remarks>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>The list of actions to queue.</returns>
				IReadOnlyList<CardAction> ProvideEvadePaymentActions(IProvideEvadePaymentActionsArgs args);
				
				/// <summary>
				/// Ran when an on-screen evade button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void EvadeButtonHovered(IEvadeButtonHoveredArgs args) { }
				
				/// <summary>
				/// The arguments for the <see cref="CanPayForEvade"/> method.
				/// </summary>
				public interface ICanPayForEvadeArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The entry of the action this payment option is used for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="ProvideEvadePaymentActions"/> method.
				/// </summary>
				public interface IProvideEvadePaymentActionsArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The entry of the action this payment option is used for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="EvadeButtonHovered"/> method.
				/// </summary>
				public interface IEvadeButtonHoveredArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The entry of the action this payment option is used for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
				}
			}

			/// <summary>
			/// Represents a precondition that needs to be satisfied before an action can take place.
			/// </summary>
			public interface IEvadePrecondition
			{
				/// <summary>
				/// Tests if this action can currently be used regarding a single condition.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>A description of whether this action can currently be used, and what to do if not.</returns>
				IResult IsEvadeAllowed(IIsEvadeAllowedArgs args);
				
				/// <summary>
				/// Ran when an on-screen evade button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void EvadeButtonHovered(IEvadeButtonHoveredArgs args) { }

				/// <summary>
				/// Describes the result of an evade precondition.
				/// </summary>
				public interface IResult
				{
					/// <summary>
					/// Whether the action is allowed.
					/// </summary>
					bool IsAllowed { get; set; }
					
					/// <summary>
					/// Whether the ship should shake if the precondition fails. Defaults to <c>true</c>.
					/// </summary>
					bool ShakeShipOnFail { get; set; }
					
					/// <summary>
					/// The actions to queue if the precondition fails. Defaults to an empty list.
					/// </summary>
					IList<CardAction> ActionsOnFail { get; set; }
					
					/// <summary>
					/// Sets <see cref="IsAllowed"/>.
					/// </summary>
					/// <param name="value">The new value.</param>
					/// <returns>This object after the change.</returns>
					IResult SetIsAllowed(bool value);
					
					/// <summary>
					/// Sets <see cref="ShakeShipOnFail"/>.
					/// </summary>
					/// <param name="value">The new value.</param>
					/// <returns>This object after the change.</returns>
					IResult SetShakeShipOnFail(bool value);
					
					/// <summary>
					/// Sets <see cref="ActionsOnFail"/>.
					/// </summary>
					/// <param name="value">The new value.</param>
					/// <returns>This object after the change.</returns>
					IResult SetActionsOnFail(IList<CardAction> value);
				}
				
				/// <summary>
				/// The arguments for the <see cref="IsEvadeAllowed"/> method.
				/// </summary>
				public interface IIsEvadeAllowedArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The entry of the action this precondition is checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that will be used to pay for the action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
					
					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					/// <remarks>
					/// This can be used to make a precondition have some side effects, without executing them during rendering.
					/// </remarks>
					bool ForRendering { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="EvadeButtonHovered"/> method.
				/// </summary>
				public interface IEvadeButtonHoveredArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The entry of the action this precondition is checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that will be used to pay for the action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
					
					/// <summary>
					/// A description of whether this action can currently be used, and what to do if not.
					/// </summary>
					IResult Result { get; }
				}
			}

			/// <summary>
			/// Represents a postcondition that needs to be satisfied before an action can take place, but after it is paid for.
			/// </summary>
			public interface IEvadePostcondition
			{
				/// <summary>
				/// Tests if this action can currently be used regarding a single condition.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>A description of whether this action can currently be used, and what to do if not.</returns>
				IResult IsEvadeAllowed(IIsEvadeAllowedArgs args);
				
				/// <summary>
				/// Ran when an on-screen evade button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void EvadeButtonHovered(IEvadeButtonHoveredArgs args) { }

				/// <summary>
				/// Describes the result of an evade postcondition.
				/// </summary>
				public interface IResult
				{
					/// <summary>
					/// Whether the action is allowed.
					/// </summary>
					bool IsAllowed { get; set; }
					
					/// <summary>
					/// Whether the ship should shake if the postcondition fails. Defaults to <c>true</c>.
					/// </summary>
					bool ShakeShipOnFail { get; set; }
					
					/// <summary>
					/// The actions to queue if the postcondition fails. Defaults to an empty list.
					/// </summary>
					IList<CardAction> ActionsOnFail { get; set; }
					
					/// <summary>
					/// Sets <see cref="IsAllowed"/>.
					/// </summary>
					/// <param name="value">The new value.</param>
					/// <returns>This object after the change.</returns>
					IResult SetIsAllowed(bool value);
					
					/// <summary>
					/// Sets <see cref="ShakeShipOnFail"/>.
					/// </summary>
					/// <param name="value">The new value.</param>
					/// <returns>This object after the change.</returns>
					IResult SetShakeShipOnFail(bool value);
					
					/// <summary>
					/// Sets <see cref="ActionsOnFail"/>.
					/// </summary>
					/// <param name="value">The new value.</param>
					/// <returns>This object after the change.</returns>
					IResult SetActionsOnFail(IList<CardAction> value);
				}
				
				/// <summary>
				/// The arguments for the <see cref="IsEvadeAllowed"/> method.
				/// </summary>
				public interface IIsEvadeAllowedArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The entry of the action this postcondition is checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that was or will be used to pay for the action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
					
					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					/// <remarks>
					/// This can be used to make a postcondition have some side effects, without executing them during rendering.
					/// </remarks>
					bool ForRendering { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="EvadeButtonHovered"/> method.
				/// </summary>
				public interface IEvadeButtonHoveredArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The entry of the action this postcondition is checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that will used to pay for the action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
					
					/// <summary>
					/// A description of whether this action can currently be used, and what to do if not.
					/// </summary>
					IResult Result { get; }
				}
			}
			
			/// <summary>
			/// A hook related to <see cref="Status.evade"/>.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Controls whether the given action is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given action is enabled. Defaults to <c>true</c>.</returns>
				bool IsEvadeActionEnabled(IIsEvadeActionEnabledArgs args) => true;
				
				/// <summary>
				/// Controls whether the given payment option is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given payment option is enabled. Defaults to <c>true</c>.</returns>
				bool IsEvadePaymentOptionEnabled(IIsEvadePaymentOptionEnabledArgs args) => true;
				
				/// <summary>
				/// Controls whether the given precondition is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given precondition is enabled. Defaults to <c>true</c>.</returns>
				bool IsEvadePreconditionEnabled(IIsEvadePreconditionEnabledArgs args) => true;
				
				/// <summary>
				/// Controls whether the given postcondition is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given postcondition is enabled. Defaults to <c>true</c>.</returns>
				bool IsEvadePostconditionEnabled(IIsEvadePostconditionEnabledArgs args) => true;
				
				/// <summary>
				/// An event called whenever any evade action fails due to a precondition.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void EvadePreconditionFailed(IEvadePreconditionFailedArgs args) { }
				
				/// <summary>
				/// An event called whenever any evade action fails due to a postcondition.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void EvadePostconditionFailed(IEvadePostconditionFailedArgs args) { }
				
				/// <summary>
				/// An event called whenever an evade action succeeds.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void AfterEvade(IAfterEvadeArgs args) { }

				/// <summary>
				/// Controls whether the default evade buttons are shown.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the button should be shown, <c>false</c> if not, <c>null</c> if the hook does not care.</returns>
				bool? ShouldShowEvadeButton(IShouldShowEvadeButtonArgs args) => null;

				/// <summary>
				/// The arguments for the <see cref="IsEvadeActionEnabled"/> hook method.
				/// </summary>
				public interface IIsEvadeActionEnabledArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The action entry being checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }

					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					bool ForRendering
						=> throw new InvalidProgramException("Should never be called directly; real implementation in Kokoro");
				}

				/// <summary>
				/// The arguments for the <see cref="IsEvadePaymentOptionEnabled"/> hook method.
				/// </summary>
				public interface IIsEvadePaymentOptionEnabledArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The action entry being checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option being checked for.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
					
					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					bool ForRendering
						=> throw new InvalidProgramException("Should never be called directly; real implementation in Kokoro");
				}

				/// <summary>
				/// The arguments for the <see cref="IsEvadePreconditionEnabled"/> hook method.
				/// </summary>
				public interface IIsEvadePreconditionEnabledArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The action entry being checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The precondition being checked for.
					/// </summary>
					IEvadePrecondition Precondition { get; }
					
					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					bool ForRendering
						=> throw new InvalidProgramException("Should never be called directly; real implementation in Kokoro");
				}

				/// <summary>
				/// The arguments for the <see cref="IsEvadePostconditionEnabled"/> hook method.
				/// </summary>
				public interface IIsEvadePostconditionEnabledArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }

					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }

					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }

					/// <summary>
					/// The action entry being checked for.
					/// </summary>
					IEvadeActionEntry Entry { get; }

					/// <summary>
					/// The payment option that would used to pay for the action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }

					/// <summary>
					/// The precondition being checked for.
					/// </summary>
					IEvadePostcondition Postcondition { get; }

					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					bool ForRendering
						=> throw new InvalidProgramException("Should never be called directly; real implementation in Kokoro");
				}

				/// <summary>
				/// The arguments for the <see cref="EvadePreconditionFailed"/> hook method.
				/// </summary>
				public interface IEvadePreconditionFailedArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The action entry that failed.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The precondition that failed.
					/// </summary>
					IEvadePrecondition Precondition { get; }
					
					/// <summary>
					/// The actions that were queued due to the failure.
					/// </summary>
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="EvadePostconditionFailed"/> hook method.
				/// </summary>
				public interface IEvadePostconditionFailedArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The action entry that failed.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that was used to pay for this action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
					
					/// <summary>
					/// The postcondition that failed.
					/// </summary>
					IEvadePostcondition Postcondition { get; }
					
					/// <summary>
					/// The actions that were queued due to the failure.
					/// </summary>
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="AfterEvade"/> hook method.
				/// </summary>
				public interface IAfterEvadeArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
					
					/// <summary>
					/// The action entry that succeeded.
					/// </summary>
					IEvadeActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that was used to pay for this action.
					/// </summary>
					IEvadePaymentOption PaymentOption { get; }
					
					/// <summary>
					/// The actions that were queued.
					/// </summary>
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="ShouldShowEvadeButton"/> hook method.
				/// </summary>
				public interface IShouldShowEvadeButtonArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The direction of movement.
					/// </summary>
					Direction Direction { get; }
				}
			}
		}
	}
}
