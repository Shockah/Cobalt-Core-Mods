using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IDroneShiftHookApi"/>
		IDroneShiftHookApi DroneShiftHook { get; }

		/// <summary>
		/// Allows modifying <see cref="Status.droneShift"/> behavior.
		/// </summary>
		public interface IDroneShiftHookApi
		{
			/// <summary>
			/// The default droneshift action.
			/// </summary>
			IDroneShiftActionEntry DefaultAction { get; }
			
			/// <summary>
			/// The default droneshift payment option, as in, paying with one <see cref="Status.droneShift"/>.
			/// </summary>
			IDroneShiftPaymentOption DefaultActionPaymentOption { get; }
			
			/// <summary>
			/// The debug droneshift payment option, as in, for free, when Debug mode is enabled and the Shift key is held.
			/// </summary>
			IDroneShiftPaymentOption DebugActionPaymentOption { get; }
			
			/// <summary>
			/// Registers a new type of action triggered by the on-screen droneshift buttons, keybinds or controller buttons.
			/// </summary>
			/// <param name="action">The new action.</param>
			/// <param name="priority">The priority for the action. Higher priority actions are called before lower priority ones. Defaults to <c>0</c>.</param>
			/// <returns>An entry representing the new action and allowing further modifications.</returns>
			IDroneShiftActionEntry RegisterAction(IDroneShiftAction action, double priority = 0);

			/// <summary>
			/// Creates a new result of a precondition.
			/// </summary>
			/// <param name="isAllowed">Whether the action is allowed.</param>
			/// <returns>The new result.</returns>
			IDroneShiftPrecondition.IResult MakePreconditionResult(bool isAllowed);
			
			/// <summary>
			/// Creates a new result of a postcondition.
			/// </summary>
			/// <param name="isAllowed">Whether the action is allowed.</param>
			/// <returns>The new result.</returns>
			IDroneShiftPostcondition.IResult MakePostconditionResult(bool isAllowed);
			
			/// <summary>
			/// Registers a new hook related to <see cref="Status.droneShift"/>.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to <see cref="Status.droneShift"/>.
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
			/// Represents an entry for a type of action triggered by the on-screen droneshift buttons, keybinds or controller buttons, and allowing further modifications.
			/// </summary>
			public interface IDroneShiftActionEntry
			{
				/// <summary>
				/// The type of action triggered by the on-screen droneshift buttons, keybinds or controller buttons.
				/// </summary>
				IDroneShiftAction Action { get; }
				
				/// <summary>
				/// A sorted enumerable over all of the payment options for this action.
				/// </summary>
				IEnumerable<IDroneShiftPaymentOption> PaymentOptions { get; }
				
				/// <summary>
				/// A sorted enumerable over all of the preconditions that need to be satisfied before this action can take place.
				/// </summary>
				IEnumerable<IDroneShiftPrecondition> Preconditions { get; }
				
				/// <summary>
				/// A sorted enumerable over all of the postconditions that need to be satisfied before this action can take place, but after it is paid for.
				/// </summary>
				IEnumerable<IDroneShiftPostcondition> Postconditions { get; }

				/// <summary>
				/// Registers a new payment option.
				/// </summary>
				/// <param name="paymentOption">The new payment option.</param>
				/// <param name="priority">The priority for the payment option. Higher priority options are called before lower priority ones. Defaults to <c>0</c>.</param>
				/// <returns>This object after the change.</returns>
				IDroneShiftActionEntry RegisterPaymentOption(IDroneShiftPaymentOption paymentOption, double priority = 0);
				
				/// <summary>
				/// Unregisters a payment option.
				/// </summary>
				/// <param name="paymentOption">The payment option.</param>
				/// <returns>This object after the change.</returns>
				IDroneShiftActionEntry UnregisterPaymentOption(IDroneShiftPaymentOption paymentOption);

				/// <summary>
				/// Registers a new precondition that needs to be satisfied before this action can take place.
				/// </summary>
				/// <param name="precondition">The new precondition.</param>
				/// <param name="priority">The priority for the precondition. Higher priority preconditions are called before lower priority ones. Defaults to <c>0</c>.</param>
				/// <returns>This object after the change.</returns>
				IDroneShiftActionEntry RegisterPrecondition(IDroneShiftPrecondition precondition, double priority = 0);
				
				/// <summary>
				/// Unregisters a precondition.
				/// </summary>
				/// <param name="precondition">The precondition.</param>
				/// <returns>This object after the change.</returns>
				IDroneShiftActionEntry UnregisterPrecondition(IDroneShiftPrecondition precondition);

				/// <summary>
				/// Registers a new postcondition that needs to be satisfied before this action can take place, but after it is paid for.
				/// </summary>
				/// <param name="postcondition">The new precondition.</param>
				/// <param name="priority">The priority for the postcondition. Higher priority postconditions are called before lower priority ones. Defaults to <c>0</c>.</param>
				/// <returns>This object after the change.</returns>
				IDroneShiftActionEntry RegisterPostcondition(IDroneShiftPostcondition postcondition, double priority = 0);
				
				/// <summary>
				/// Unregisters a postcondition.
				/// </summary>
				/// <param name="postcondition">The postcondition.</param>
				/// <returns>This object after the change.</returns>
				IDroneShiftActionEntry UnregisterPostcondition(IDroneShiftPostcondition postcondition);
			}
			
			/// <summary>
			/// Represents a type of action triggered by the on-screen droneshift buttons, keybinds or controller buttons.
			/// </summary>
			public interface IDroneShiftAction
			{
				/// <summary>
				/// Tests if this action can currently be used.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>Whether this action can currently be used.</returns>
				bool CanDoDroneShiftAction(ICanDoDroneShiftArgs args);
				
				/// <summary>
				/// Provides a list of actions to queue related to a single action trigger.
				/// </summary>
				/// <remarks>
				/// It is allowed for this method to execute non-action code and return an empty action list.
				/// </remarks>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>The list of actions to queue.</returns>
				IReadOnlyList<CardAction> ProvideDroneShiftActions(IProvideDroneShiftActionsArgs args);
				
				/// <summary>
				/// Ran when an on-screen droneshift button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }

				/// <summary>
				/// The arguments for the <see cref="CanDoDroneShiftAction"/> method.
				/// </summary>
				public interface ICanDoDroneShiftArgs
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
				/// The arguments for the <see cref="ProvideDroneShiftActions"/> method.
				/// </summary>
				public interface IProvideDroneShiftActionsArgs
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
					IDroneShiftPaymentOption PaymentOption { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="DroneShiftButtonHovered"/> method.
				/// </summary>
				public interface IDroneShiftButtonHoveredArgs
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
			/// Represents a way of paying for an action triggered by the on-screen droneshift buttons, keybinds or controller buttons.
			/// </summary>
			/// <remarks>
			/// An action requires at least one payment option, or otherwise it cannot be triggered.
			/// </remarks>
			public interface IDroneShiftPaymentOption
			{
				/// <summary>
				/// Tests if this payment option can currently be used.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>Whether this payment option can currently be used.</returns>
				bool CanPayForDroneShift(ICanPayForDroneShiftArgs args);
				
				/// <summary>
				/// Provides a list of actions to queue related to a single payment.
				/// </summary>
				/// <remarks>
				/// It is allowed for this method to execute non-action payments and return an empty action list.
				/// </remarks>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>The list of actions to queue.</returns>
				IReadOnlyList<CardAction> ProvideDroneShiftPaymentActions(IProvideDroneShiftPaymentActionsArgs args);
				
				/// <summary>
				/// Ran when an on-screen droneshift button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }
				
				/// <summary>
				/// The arguments for the <see cref="CanPayForDroneShift"/> method.
				/// </summary>
				public interface ICanPayForDroneShiftArgs
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
					IDroneShiftActionEntry Entry { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="ProvideDroneShiftPaymentActions"/> method.
				/// </summary>
				public interface IProvideDroneShiftPaymentActionsArgs
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
					IDroneShiftActionEntry Entry { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="DroneShiftButtonHovered"/> method.
				/// </summary>
				public interface IDroneShiftButtonHoveredArgs
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
					IDroneShiftActionEntry Entry { get; }
				}
			}

			/// <summary>
			/// Represents a precondition that needs to be satisfied before an action can take place.
			/// </summary>
			public interface IDroneShiftPrecondition
			{
				/// <summary>
				/// Tests if this action can currently be used regarding a single condition.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>A description of whether this action can currently be used, and what to do if not.</returns>
				IResult IsDroneShiftAllowed(IIsDroneShiftAllowedArgs args);
				
				/// <summary>
				/// Ran when an on-screen droneshift button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }

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
				/// The arguments for the <see cref="IsDroneShiftAllowed"/> method.
				/// </summary>
				public interface IIsDroneShiftAllowedArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					/// <remarks>
					/// This can be used to make a precondition have some side effects, without executing them during rendering.
					/// </remarks>
					bool ForRendering { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="DroneShiftButtonHovered"/> method.
				/// </summary>
				public interface IDroneShiftButtonHoveredArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// A description of whether this action can currently be used, and what to do if not.
					/// </summary>
					IResult Result { get; }
				}
			}

			/// <summary>
			/// Represents a postcondition that needs to be satisfied before an action can take place, but after it is paid for.
			/// </summary>
			public interface IDroneShiftPostcondition
			{
				/// <summary>
				/// Tests if this action can currently be used regarding a single condition.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>A description of whether this action can currently be used, and what to do if not.</returns>
				IResult IsDroneShiftAllowed(IIsDroneShiftAllowedArgs args);
				
				/// <summary>
				/// Ran when an on-screen droneshift button is hovered, allowing any custom visuals to be implemented, like highlighting a status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				void DroneShiftButtonHovered(IDroneShiftButtonHoveredArgs args) { }

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
				/// The arguments for the <see cref="IsDroneShiftAllowed"/> method.
				/// </summary>
				public interface IIsDroneShiftAllowedArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that was or will be used to pay for the action.
					/// </summary>
					IDroneShiftPaymentOption PaymentOption { get; }
					
					/// <summary>
					/// Whether this method was called for rendering purposes, or actual action purposes otherwise.
					/// </summary>
					/// <remarks>
					/// This can be used to make a postcondition have some side effects, without executing them during rendering.
					/// </remarks>
					bool ForRendering { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="DroneShiftButtonHovered"/> method.
				/// </summary>
				public interface IDroneShiftButtonHoveredArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that will used to pay for the action.
					/// </summary>
					IDroneShiftPaymentOption PaymentOption { get; }
					
					/// <summary>
					/// A description of whether this action can currently be used, and what to do if not.
					/// </summary>
					IResult Result { get; }
				}
			}
			
			/// <summary>
			/// A hook related to <see cref="Status.droneShift"/>.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Controls whether the given action is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given action is enabled. Defaults to <c>true</c>.</returns>
				bool IsDroneShiftActionEnabled(IIsDroneShiftActionEnabledArgs args) => true;
				
				/// <summary>
				/// Controls whether the given payment option is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given payment option is enabled. Defaults to <c>true</c>.</returns>
				bool IsDroneShiftPaymentOptionEnabled(IIsDroneShiftPaymentOptionEnabledArgs args) => true;
				
				/// <summary>
				/// Controls whether the given precondition is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given precondition is enabled. Defaults to <c>true</c>.</returns>
				bool IsDroneShiftPreconditionEnabled(IIsDroneShiftPreconditionEnabledArgs args) => true;
				
				/// <summary>
				/// Controls whether the given postcondition is enabled.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the given postcondition is enabled. Defaults to <c>true</c>.</returns>
				bool IsDroneShiftPostconditionEnabled(IIsDroneShiftPostconditionEnabledArgs args) => true;
				
				/// <summary>
				/// An event called whenever any droneshift action fails due to a precondition.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void DroneShiftPreconditionFailed(IDroneShiftPreconditionFailedArgs args) { }
				
				/// <summary>
				/// An event called whenever any droneshift action fails due to a postcondition.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void DroneShiftPostconditionFailed(IDroneShiftPostconditionFailedArgs args) { }
				
				/// <summary>
				/// An event called whenever an droneshift action succeeds.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void AfterDroneShift(IAfterDroneShiftArgs args) { }

				/// <summary>
				/// The arguments for the <see cref="IsDroneShiftActionEnabled"/> hook method.
				/// </summary>
				public interface IIsDroneShiftActionEnabledArgs
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
					IDroneShiftActionEntry Entry { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="IsDroneShiftPaymentOptionEnabled"/> hook method.
				/// </summary>
				public interface IIsDroneShiftPaymentOptionEnabledArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option being checked for.
					/// </summary>
					IDroneShiftPaymentOption PaymentOption { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="IsDroneShiftPreconditionEnabled"/> hook method.
				/// </summary>
				public interface IIsDroneShiftPreconditionEnabledArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The precondition being checked for.
					/// </summary>
					IDroneShiftPrecondition Precondition { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="IsDroneShiftPostconditionEnabled"/> hook method.
				/// </summary>
				public interface IIsDroneShiftPostconditionEnabledArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that would used to pay for the action.
					/// </summary>
					IDroneShiftPaymentOption PaymentOption { get; }
					
					/// <summary>
					/// The precondition being checked for.
					/// </summary>
					IDroneShiftPostcondition Postcondition { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="DroneShiftPreconditionFailed"/> hook method.
				/// </summary>
				public interface IDroneShiftPreconditionFailedArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The precondition that failed.
					/// </summary>
					IDroneShiftPrecondition Precondition { get; }
					
					/// <summary>
					/// The actions that were queued due to the failure.
					/// </summary>
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="DroneShiftPostconditionFailed"/> hook method.
				/// </summary>
				public interface IDroneShiftPostconditionFailedArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that was used to pay for this action.
					/// </summary>
					IDroneShiftPaymentOption PaymentOption { get; }
					
					/// <summary>
					/// The postcondition that failed.
					/// </summary>
					IDroneShiftPostcondition Postcondition { get; }
					
					/// <summary>
					/// The actions that were queued due to the failure.
					/// </summary>
					IReadOnlyList<CardAction> QueuedActions { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="AfterDroneShift"/> hook method.
				/// </summary>
				public interface IAfterDroneShiftArgs
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
					IDroneShiftActionEntry Entry { get; }
					
					/// <summary>
					/// The payment option that was used to pay for this action.
					/// </summary>
					IDroneShiftPaymentOption PaymentOption { get; }
					
					/// <summary>
					/// The actions that were queued.
					/// </summary>
					IReadOnlyList<CardAction> QueuedActions { get; }
				}
			}
		}
	}
}
