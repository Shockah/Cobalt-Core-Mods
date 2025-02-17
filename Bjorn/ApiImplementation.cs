using System.Collections.Generic;

namespace Shockah.Bjorn;

public sealed class ApiImplementation : IBjornApi
{
	public void RegisterHook(IBjornApi.IHook hook, double priority = 0)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IBjornApi.IHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
	
	internal sealed class CanAnalyzeArgs : IBjornApi.IHook.ICanAnalyzeArgs
	{
		public State State { get; internal set; } = null!;
		public Combat Combat { get; internal set; } = null!;
		public Card Card { get; internal set; } = null!;
	}
	
	internal sealed class IsCanAnalyzeHookEnabledArgs : IBjornApi.IHook.IIsCanAnalyzeHookEnabledArgs
	{
		public State State { get; internal set; } = null!;
		public Combat Combat { get; internal set; } = null!;
		public Card Card { get; internal set; } = null!;
		public IBjornApi.IHook Hook { get; internal set; } = null!;
	}
	
	internal sealed class OnCardsAnalyzedArgs : IBjornApi.IHook.IOnCardsAnalyzedArgs
	{
		public State State { get; internal set; } = null!;
		public Combat Combat { get; internal set; } = null!;
		public IReadOnlyList<Card> Cards { get; internal set; } = null!;
		public bool Permanent { get; internal set; }
	}
	
	internal sealed class ModifyRelativityLimitArgs : IBjornApi.IHook.IModifyRelativityLimitArgs
	{
		public State State { get; internal set; } = null!;
		public Combat Combat { get; internal set; } = null!;
		public int BaseLimit { get; internal set; }
		public int Limit { get; set; }
	}
}