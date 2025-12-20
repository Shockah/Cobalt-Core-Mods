using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IStatusLogicApi"/>
		IStatusLogicApi StatusLogic { get; }

		/// <summary>
		/// Allows modifying how a status behaves via a hook.
		/// </summary>
		public interface IStatusLogicApi
		{
			/// <summary>
			/// Registers a new hook related to status logic.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c></param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to status logic.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);

			/// <summary>
			/// Checks whether a given status can be immediately triggered.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="targetPlayer">Whether the status should be triggered for the player (<c>true</c>) or the enemy (<c>false</c>).</param>
			/// <param name="status">The status to trigger.</param>
			/// <returns>Whether the status can be immediately triggered.</returns>
			bool CanImmediatelyTriggerStatus(State state, Combat combat, bool targetPlayer, Status status);

			/// <summary>
			/// Immediately triggers a status, if possible.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="targetPlayer">Whether the status should be triggered for the player (<c>true</c>) or the enemy (<c>false</c>).</param>
			/// <param name="status">The status to trigger.</param>
			/// <param name="keepAmount">
			/// Whether the amount of the status should be kept in-tact.
			/// When <c>true</c>, ignores any changes to <see cref="IHook.IHandleImmediateStatusTriggerArgs.NewAmount"/> or <see cref="IHook.IHandleImmediateStatusTriggerArgs.SetStrategy"/>.
			/// </param>
			/// <returns>Whether the status was successfully triggered.</returns>
			bool ImmediatelyTriggerStatus(State state, Combat combat, bool targetPlayer, Status status, bool keepAmount = false);
			
			/// <summary>
			/// Casts the action as a status trigger action, if it is one.
			/// </summary>
			/// <param name="action">The potential status trigger action.</param>
			/// <returns>The status trigger action, if the given action is one, or <c>null</c> otherwise.</returns>
			ITriggerStatusAction? AsTriggerStatusAction(CardAction action);
			
			/// <summary>
			/// Creates a new status trigger action.
			/// </summary>
			/// <param name="targetPlayer">Whether the status should be triggered for the player (<c>true</c>) or the enemy (<c>false</c>).</param>
			/// <param name="status">The status to trigger immediately.</param>
			/// <returns>The new status trigger action.</returns>
			ITriggerStatusAction MakeTriggerStatusAction(bool targetPlayer, Status status);
			
			/// <summary>
			/// Describes the current timing of the turn.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum StatusTurnTriggerTiming
			{
				/// <summary>
				/// The start of a turn.
				/// </summary>
				TurnStart,
				
				/// <summary>
				/// The end of a turn.
				/// </summary>
				TurnEnd
			}

			/// <summary>
			/// Describes the strategy to use for changing the amount of a status.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum StatusTurnAutoStepSetStrategy
			{
				/// <summary>
				/// Set the value of a status directly via <see cref="Ship.Set"/>.
				/// </summary>
				Direct,
				
				/// <summary>
				/// <see cref="Combat.Queue(CardAction)">Queue</see> an <see cref="AStatus"/> action with the <see cref="AStatusMode.Set"/> mode.
				/// </summary>
				QueueSet,
				
				/// <summary>
				/// <see cref="Combat.Queue(CardAction)">Queue</see> an <see cref="AStatus"/> action with the <see cref="AStatusMode.Add"/> mode.
				/// </summary>
				QueueAdd,
				
				/// <summary>
				/// <see cref="Combat.QueueImmediate(CardAction)">Queue immediately</see> an <see cref="AStatus"/> action with the <see cref="AStatusMode.Set"/> mode.
				/// </summary>
				QueueImmediateSet,
				
				/// <summary>
				/// <see cref="Combat.QueueImmediate(CardAction)">Queue immediately</see> an <see cref="AStatus"/> action with the <see cref="AStatusMode.Add"/> mode.
				/// </summary>
				QueueImmediateAdd
			}
			
			/// <summary>
			/// A hook related to status logic.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Modifies the amount to which a status changes.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The modified new amount of the status. Defaults to <see cref="IModifyStatusChangeArgs.NewAmount"/>.</returns>
				int ModifyStatusChange(IModifyStatusChangeArgs args) => args.NewAmount;
				
				/// <summary>
				/// Controls whether the given status is affected by <see cref="Status.boost"/>.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the status should be affected by <see cref="Status.boost"/>, <c>false</c> if not, <c>null</c> if this hook does not care. Defaults to <c>null</c>.</returns>
				bool? IsAffectedByBoost(IIsAffectedByBoostArgs args) => null;

				/// <summary>
				/// Controls the statuses for which the <see cref="ModifyStatusTurnTriggerPriority"/>, <see cref="OnStatusTurnTrigger"/> and <see cref="HandleStatusTurnAutoStep"/> methods will be called.
				/// Defaults to <c>args.NonZeroStatuses</c>.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The set of statuses other hooks will be called for.</returns>
				IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IGetStatusesToCallTurnTriggerHooksForArgs args) => args.NonZeroStatuses;

				/// <summary>
				/// Modifies the priority at which a status triggers at the start and end of every turn.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The priority. Higher priority statuses are handled before lower priority ones. Defaults to <see cref="IModifyStatusTurnTriggerPriorityArgs.Priority"/>.</returns>
				double ModifyStatusTurnTriggerPriority(IModifyStatusTurnTriggerPriorityArgs args) => args.Priority;
				
				/// <summary>
				/// An event called for every status at the start and end of every turn.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void OnStatusTurnTrigger(IOnStatusTurnTriggerArgs args) { }
				
				/// <summary>
				/// Modifies how the given status behaves at the start and end of every turn.
				/// This method can be used to make a status tick down/up.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether this hook handled the given status and no further hooks should be called. Defaults to <c>false</c>.</returns>
				bool HandleStatusTurnAutoStep(IHandleStatusTurnAutoStepArgs args) => false;

				/// <summary>
				/// Controls whether the hook can handle an immediate trigger of a given status.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the hook can handle an immediate trigger of the status, <c>false</c> otherwise. Defaults to <c>false</c>.</returns>
				/// <seealso cref="HandleImmediateStatusTrigger"/>
				bool CanHandleImmediateStatusTrigger(ICanHandleImmediateStatusTriggerArgs args) => false;
				
				/// <summary>
				/// Allows a hook to handle an immediate trigger of a given status.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <seealso cref="CanHandleImmediateStatusTrigger"/>
				void HandleImmediateStatusTrigger(IHandleImmediateStatusTriggerArgs args) { }
				
				/// <summary>
				/// The arguments for the <see cref="ModifyStatusChange"/> hook method.
				/// </summary>
				public interface IModifyStatusChangeArgs
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
					/// The ship the status is getting changed for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status being changed.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The old amount of the status.
					/// </summary>
					int OldAmount { get; }
					
					/// <summary>
					/// The new amount of the status.
					/// </summary>
					int NewAmount { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="IsAffectedByBoost"/> hook method.
				/// </summary>
				public interface IIsAffectedByBoostArgs
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
					/// The ship the status is getting changed for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status being changed.
					/// </summary>
					Status Status { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="IHook.GetStatusesToCallTurnTriggerHooksFor"/> hook method.
				/// </summary>
				public interface IGetStatusesToCallTurnTriggerHooksForArgs
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
					/// The current timing of the turn.
					/// </summary>
					StatusTurnTriggerTiming Timing { get; }

					/// <summary>
					/// The ship the status is getting triggered for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// All existing statuses in the game.
					/// </summary>
					IReadOnlySet<Status> KnownStatuses { get; }
					
					/// <summary>
					/// All statuses the ship currently has a non-zero amount of.
					/// </summary>
					IReadOnlySet<Status> NonZeroStatuses { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="ModifyStatusTurnTriggerPriority"/> hook method.
				/// </summary>
				public interface IModifyStatusTurnTriggerPriorityArgs
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
					/// The current timing of the turn.
					/// </summary>
					StatusTurnTriggerTiming Timing { get; }

					/// <summary>
					/// The ship the status is getting triggered for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status being changed.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The current amount of the status.
					/// </summary>
					int Amount { get; }
					
					/// <summary>
					/// The current priority. Higher priority statuses are handled before lower priority ones. Defaults to <c>0</c>.
					/// </summary>
					double Priority { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="OnStatusTurnTrigger"/> hook method.
				/// </summary>
				public interface IOnStatusTurnTriggerArgs
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
					/// The current timing of the turn.
					/// </summary>
					StatusTurnTriggerTiming Timing { get; }
					
					/// <summary>
					/// The ship the turn was for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status currently being checked.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The old amount of the status.
					/// </summary>
					int OldAmount { get; }
					
					/// <summary>
					/// The new amount of the status.
					/// </summary>
					int NewAmount { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="HandleStatusTurnAutoStep"/> hook method.
				/// </summary>
				public interface IHandleStatusTurnAutoStepArgs
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
					/// The current timing of the turn.
					/// </summary>
					StatusTurnTriggerTiming Timing { get; }
					
					/// <summary>
					/// The ship the turn was for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status currently being handled.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The current amount of the status.
					/// This value can be modified by the hook.
					/// </summary>
					int Amount { get; set; }
					
					/// <summary>
					/// The strategy to use for changing the amount of the status.
					/// This value can be modified by the hook.
					/// </summary>
					StatusTurnAutoStepSetStrategy SetStrategy { get; set; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="CanHandleImmediateStatusTrigger"/> hook method.
				/// </summary>
				public interface ICanHandleImmediateStatusTriggerArgs
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
					/// The ship the turn was for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status currently being handled.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The current amount of the status.
					/// </summary>
					int Amount { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="HandleImmediateStatusTrigger"/> hook method.
				/// </summary>
				public interface IHandleImmediateStatusTriggerArgs
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
					/// The ship the turn was for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status currently being handled.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// Whether it was requested the amount of the status should be kept in-tact.
					/// When <c>true</c>, ignores any changes to <see cref="NewAmount"/> or <see cref="SetStrategy"/>.
					/// </summary>
					bool KeepAmount { get; }
					
					/// <summary>
					/// The old amount of the status.
					/// </summary>
					int OldAmount { get; }
					
					/// <summary>
					/// The new amount of the status.
					/// This value can be modified by the hook.
					/// </summary>
					int NewAmount { get; set; }
					
					/// <summary>
					/// The strategy to use for changing the amount of the status.
					/// This value can be modified by the hook.
					/// </summary>
					StatusTurnAutoStepSetStrategy SetStrategy { get; set; }
				}
			}
			
			/// <summary>
			/// Represents an action which immediately triggers a status.
			/// </summary>
			public interface ITriggerStatusAction : ICardAction<CardAction>
			{
				/// <summary>
				/// Whether the status should be triggered for the player (<c>true</c>) or the enemy (<c>false</c>).
				/// </summary>
				bool TargetPlayer { get; set; }
				
				/// <summary>
				/// The status to trigger immediately.
				/// </summary>
				Status Status { get; set; }
				
				/// <summary>
				/// Whether the status' amount should be kept as-is. Defaults to <c>false</c>.
				/// </summary>
				bool KeepAmount { get; set; }
				
				/// <summary>
				/// An optional action to run if the trigger succeeded.
				/// </summary>
				CardAction? SuccessAction { get; set; }

				/// <summary>
				/// Sets <see cref="TargetPlayer"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITriggerStatusAction SetTargetPlayer(bool value);

				/// <summary>
				/// Sets <see cref="Status"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITriggerStatusAction SetStatus(Status value);

				/// <summary>
				/// Sets <see cref="KeepAmount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITriggerStatusAction SetKeepAmount(bool value);

				/// <summary>
				/// Sets <see cref="SuccessAction"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITriggerStatusAction SetSuccessAction(CardAction? value);
			}
		}
	}
}
