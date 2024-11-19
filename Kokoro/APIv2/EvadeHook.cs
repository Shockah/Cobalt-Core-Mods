using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IEvadeHookApi EvadeHook { get; }

		public interface IEvadeHookApi
		{
			IHook VanillaEvadeHook { get; }
			IHook VanillaDebugEvadeHook { get; }
			
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public enum EvadeHookContext
			{
				Rendering, Action
			}
			
			public interface IHook : IKokoroV2ApiHook
			{
				bool? IsEvadePossible(IIsEvadePossibleArgs args) => null;
				void PayForEvade(IPayForEvadeArgs args) { }
				void AfterEvade(IAfterEvadeArgs args) { }
				List<CardAction>? ProvideEvadeActions(IProvideEvadeActionsArgs args) => null;
				
				public interface IIsEvadePossibleArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
					EvadeHookContext Context { get; }
				}
				
				public interface IPayForEvadeArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
				}
				
				public interface IAfterEvadeArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
					IHook Hook { get; }
				}
				
				public interface IProvideEvadeActionsArgs
				{
					State State { get; }
					Combat Combat { get; }
					int Direction { get; }
				}
			}
		}
	}
}
