using Nickel;
using Shockah.Kokoro;

namespace Shockah.Wade;

public sealed class ApiImplementation : IWadeApi
{
	public IDeckEntry WadeDeck
		=> ModEntry.Instance.WadeDeck;

	public IStatusEntry OddsStatus
		=> Odds.OddsStatus;
	
	public IStatusEntry RedTrendStatus
		=> Odds.RedTrendStatus;
	
	public IStatusEntry GreenTrendStatus
		=> Odds.GreenTrendStatus;
	
	public IStatusEntry LuckyDriveStatus
		=> Odds.LuckyDriveStatus;

	public IWadeApi.ITrendCondition MakeTrendCondition(bool positive)
		=> new Odds.TrendCondition { Positive = true };

	public IWadeApi.ITrendCondition? AsTrendCondition(IKokoroApi.IV2.IConditionalApi.IBoolExpression condition)
		=> condition as IWadeApi.ITrendCondition;

	public IWadeApi.IRollAction MakeRollAction()
		=> new Odds.RollAction();

	public IWadeApi.IRollAction? AsRollAction(CardAction action)
		=> action as IWadeApi.IRollAction;

	public void RegisterHook(IWadeApi.IHook hook, double priority = 0)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IWadeApi.IHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}