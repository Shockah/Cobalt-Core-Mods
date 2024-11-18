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
			
			public interface IHook
			{
				int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount) => newAmount;
				bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status) => null;
				void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount) { }
				bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy) => false;
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
