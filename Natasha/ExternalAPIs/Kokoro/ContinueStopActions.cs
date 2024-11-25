using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IContinueStopApi"/>
		IContinueStopApi ContinueStop { get; }

		/// <summary>
		/// Allows working with and creating continue/stop actions.
		/// </summary>
		public interface IContinueStopApi
		{
			/// <summary>
			/// Casts the action to <see cref="ITriggerAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="ITriggerAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			ITriggerAction? AsTriggerAction(CardAction action);
			
			/// <summary>
			/// Creates a new action that will trigger a continue/stop flag.
			/// </summary>
			/// <param name="type">The type of flag to trigger.</param>
			/// <param name="id">The ID of the flag trigger.</param>
			/// <returns>A new flag trigger action.</returns>
			ITriggerAction MakeTriggerAction(ActionType type, out Guid id);
			
			/// <summary>
			/// Casts the action to <see cref="ITriggerAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="ITriggerAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IFlaggedAction? AsFlaggedAction(CardAction action);
			
			/// <summary>
			/// Creates a new action that will only run if the provided flag has been triggered.
			/// </summary>
			/// <param name="type">The type of flag to trigger.</param>
			/// <param name="id">The ID of the flag that needs to be triggered.</param>
			/// <param name="action">The action to run.</param>
			/// <returns>A new flagged action.</returns>
			IFlaggedAction MakeFlaggedAction(ActionType type, Guid id, CardAction action);
			
			/// <summary>
			/// Creates a new action that will only run if any/all of the provided flags have been triggered.
			/// </summary>
			/// <param name="type">The type of flag to trigger.</param>
			/// <param name="ids">The IDs of the flags that need to be triggered.</param>
			/// <param name="action">The action to run.</param>
			/// <returns>A new flagged action.</returns>
			IFlaggedAction MakeFlaggedAction(ActionType type, IEnumerable<Guid> ids, CardAction action);
			
			/// <summary>
			/// Creates multiple new actions that will only run if the provided flag has been triggered.
			/// </summary>
			/// <param name="type">The type of flag to trigger.</param>
			/// <param name="id">The ID of the flag that needs to be triggered.</param>
			/// <param name="actions">The actions to run.</param>
			/// <returns>An enumerable over new flagged actions.</returns>
			IEnumerable<IFlaggedAction> MakeFlaggedActions(ActionType type, Guid id, IEnumerable<CardAction> actions);
			
			/// <summary>
			/// Creates multiple new actions that will only run if any/all of the provided flags have been triggered.
			/// </summary>
			/// <param name="type">The type of flag to trigger.</param>
			/// <param name="ids">The IDs of the flags that need to be triggered.</param>
			/// <param name="actions">The actions to run.</param>
			/// <returns>An enumerable over new flagged actions.</returns>
			IEnumerable<IFlaggedAction> MakeFlaggedActions(ActionType type, IEnumerable<Guid> ids, IEnumerable<CardAction> actions);
			
			/// <summary>
			/// Represents an action that will trigger a continue/stop flag.
			/// </summary>
			public interface ITriggerAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The type of flag to trigger.
				/// </summary>
				ActionType Type { get; set; }
				
				/// <summary>
				/// The ID of the flag trigger.
				/// </summary>
				Guid Id { get; set; }

				/// <summary>
				/// Sets <see cref="Type"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITriggerAction SetType(ActionType value);
				
				/// <summary>
				/// Sets <see cref="Id"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITriggerAction SetId(Guid value);
			}

			/// <summary>
			/// Represents an action that will only run if any/all of the provided flags have been triggered.
			/// </summary>
			public interface IFlaggedAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The type of flag to trigger.
				/// </summary>
				ActionType Type { get; set; }
				
				/// <summary>
				/// The IDs of the flags that need to be triggered.
				/// </summary>
				ISet<Guid> Ids { get; set; }
				
				/// <summary>
				/// The operator to check the triggered flags with.
				/// </summary>
				FlagOperator Operator { get; set; }
				
				/// <summary>
				/// The action to run.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Sets <see cref="Type"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IFlaggedAction SetType(ActionType value);
				
				/// <summary>
				/// Sets <see cref="Ids"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IFlaggedAction SetIds(HashSet<Guid> value);
				
				/// <summary>
				/// Sets <see cref="Operator"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IFlaggedAction SetOperator(FlagOperator value);
				
				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IFlaggedAction SetAction(CardAction value);
			}

			/// <summary>
			/// The type of flag to trigger.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum ActionType
			{
				/// <summary>
				/// The action will only run if the flags are triggered.
				/// </summary>
				Continue,
				
				/// <summary>
				/// The action will run, unless the flags are triggered.
				/// </summary>
				Stop
			}

			/// <summary>
			/// The operator to check the triggered flags with.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum FlagOperator
			{
				/// <summary>
				/// All provided flags need to be triggered.
				/// </summary>
				And,
				
				/// <summary>
				/// Any of the provided flags needs to be triggered.
				/// </summary>
				Or,
				
				/// <summary>
				/// Exactly one of the provided flags needs to be triggered.
				/// </summary>
				Single
			}
		}
	}
}
