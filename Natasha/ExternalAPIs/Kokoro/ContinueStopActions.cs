using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IContinueStopApi ContinueStop { get; }

		public interface IContinueStopApi
		{
			ITriggerAction? AsTriggerAction(CardAction action);
			ITriggerAction MakeTriggerAction(ActionType type, out Guid id);
			
			IFlaggedAction? AsFlaggedAction(CardAction action);
			IFlaggedAction MakeFlaggedAction(ActionType type, Guid id, CardAction action);
			IFlaggedAction MakeFlaggedAction(ActionType type, IEnumerable<Guid> ids, CardAction action);
			IEnumerable<IFlaggedAction> MakeFlaggedActions(ActionType type, Guid id, IEnumerable<CardAction> actions);
			IEnumerable<IFlaggedAction> MakeFlaggedActions(ActionType type, IEnumerable<Guid> ids, IEnumerable<CardAction> actions);
			
			public interface ITriggerAction : ICardAction<CardAction>
			{
				ActionType Type { get; set; }
				Guid Id { get; set; }

				ITriggerAction SetType(ActionType value);
				ITriggerAction SetId(Guid value);
			}

			public interface IFlaggedAction : ICardAction<CardAction>
			{
				ActionType Type { get; set; }
				HashSet<Guid> Ids { get; set; }
				FlagOperator Operator { get; set; }
				CardAction Action { get; set; }
				
				IFlaggedAction SetType(ActionType value);
				IFlaggedAction SetIds(HashSet<Guid> value);
				IFlaggedAction SetOperator(FlagOperator value);
				IFlaggedAction SetAction(CardAction value);
			}

			public enum ActionType
			{
				Continue, Stop
			}

			public enum FlagOperator
			{
				And, Or
			}
		}
	}
}
