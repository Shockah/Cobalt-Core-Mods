using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IDroneShiftHookApi DroneShiftHook { get; }

		public interface IDroneShiftHookApi
		{
			IHook VanillaDroneShiftHook { get; }
			IHook VanillaDebugDroneShiftHook { get; }
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public enum DroneShiftHookContext
			{
				Rendering, Action
			}
			
			public interface IHook : IKokoroV2ApiHook
			{
				bool? IsDroneShiftPossible(IIsDroneShiftPossibleArgs args) => null;
				void PayForDroneShift(IPayForDroneShiftArgs args) { }
				void AfterDroneShift(IAfterDroneShiftArgs args) { }
				List<CardAction>? ProvideDroneShiftActions(IProvideDroneShiftActionsArgs args) => null;
				
				public interface IIsDroneShiftPossibleArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
					DroneShiftHookContext Context { get; }
				}
				
				public interface IPayForDroneShiftArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
				}
				
				public interface IAfterDroneShiftArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
					IHook Hook { get; }
				}
				
				public interface IProvideDroneShiftActionsArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
				}
			}
		}
	}
}
