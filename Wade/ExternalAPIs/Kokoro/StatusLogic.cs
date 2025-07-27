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
			}
		}
	}
}
