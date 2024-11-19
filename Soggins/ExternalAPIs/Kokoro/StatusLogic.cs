using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IStatusLogicApi StatusLogic { get; }

		public interface IStatusLogicApi
		{
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public interface IHook : IKokoroV2ApiHook
			{
				int ModifyStatusChange(IModifyStatusChangeArgs args) => args.NewAmount;
				bool? IsAffectedByBoost(IIsAffectedByBoostArgs args) => null;
				void OnStatusTurnTrigger(IOnStatusTurnTriggerArgs args) { }
				bool HandleStatusTurnAutoStep(IHandleStatusTurnAutoStepArgs args) => false;
				
				public interface IModifyStatusChangeArgs
				{
					State State { get; }
					Combat Combat { get; }
					Ship Ship { get; }
					Status Status { get; }
					int OldAmount { get; }
					int NewAmount { get; }
				}
				
				public interface IIsAffectedByBoostArgs
				{
					State State { get; }
					Combat Combat { get; }
					Ship Ship { get; }
					Status Status { get; }
				}
				
				public interface IOnStatusTurnTriggerArgs
				{
					State State { get; }
					Combat Combat { get; }
					StatusTurnTriggerTiming Timing { get; }
					Ship Ship { get; }
					Status Status { get; }
					int OldAmount { get; }
					int NewAmount { get; }
				}
				
				public interface IHandleStatusTurnAutoStepArgs
				{
					State State { get; }
					Combat Combat { get; }
					StatusTurnTriggerTiming Timing { get; }
					Ship Ship { get; }
					Status Status { get; }
					int Amount { get; set; }
					StatusTurnAutoStepSetStrategy SetStrategy { get; set; }
				}
			}
			
			[JsonConverter(typeof(StringEnumConverter))]
			public enum StatusTurnTriggerTiming
			{
				TurnStart, TurnEnd
			}

			[JsonConverter(typeof(StringEnumConverter))]
			public enum StatusTurnAutoStepSetStrategy
			{
				Direct, QueueSet, QueueAdd, QueueImmediateSet, QueueImmediateAdd
			}
		}
	}
}
