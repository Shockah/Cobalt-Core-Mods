using System.Collections.Generic;

namespace Shockah.Bjorn;

public interface IBjornApi
{
	void RegisterHook(IHook hook, double priority = 0);
	void UnregisterHook(IHook hook);
	
	public interface IHook
	{
		bool CanAnalyze(ICanAnalyzeArgs args) => false;
		bool IsCanAnalyzeHookEnabled(IIsCanAnalyzeHookEnabledArgs args) => true;
		void OnCardsAnalyzed(IOnCardsAnalyzedArgs args) { }
		void ModifyRelativityLimit(IModifyRelativityLimitArgs args) { }
		
		public interface ICanAnalyzeArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card Card { get; }
		}
		
		public interface IIsCanAnalyzeHookEnabledArgs
		{
			State State { get; }
			Combat Combat { get; }
			Card Card { get; }
			IHook Hook { get; }
		}

		public interface IOnCardsAnalyzedArgs
		{
			State State { get; }
			Combat Combat { get; }
			IReadOnlyList<Card> Cards { get; }
			bool Permanent { get; }
		}

		public interface IModifyRelativityLimitArgs
		{
			State State { get; }
			Combat Combat { get; }
			int BaseLimit { get; }
			int Limit { get; set; }
		}
	}
}