using Shockah.Kokoro;

namespace Shockah.DuoArtifacts;

internal sealed class PeriRiggsArtifact : DuoArtifact, IKokoroApi.IV2.IEvadeHookApi.IHook
{
	private const int EvadesPerTurn = 2;

	public int EvadesLeft = EvadesPerTurn;

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.QueueImmediate(new AStatus
		{
			status = Status.strafe,
			statusAmount = 1,
			targetPlayer = true
		});
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		EvadesLeft = EvadesPerTurn;
	}

	public override int? GetDisplayNumber(State s)
		=> EvadesLeft;

	public void AfterEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IAfterEvadeArgs args)
	{
		if (Instance.Helper.Utilities.Unproxy(args.Entry) != Instance.Helper.Utilities.Unproxy(Instance.KokoroApi.EvadeHook.DefaultAction))
			return;
		if (Instance.Helper.Utilities.Unproxy(args.PaymentOption) != Instance.Helper.Utilities.Unproxy(Instance.KokoroApi.EvadeHook.DefaultActionPaymentOption))
			return;
		
		EvadesLeft--;
	}

	public void EvadePostconditionFailed(IKokoroApi.IV2.IEvadeHookApi.IHook.IEvadePostconditionFailedArgs args)
	{
		if (Instance.Helper.Utilities.Unproxy(args.Entry) != Instance.Helper.Utilities.Unproxy(Instance.KokoroApi.EvadeHook.DefaultAction))
			return;
		if (Instance.Helper.Utilities.Unproxy(args.PaymentOption) != Instance.Helper.Utilities.Unproxy(Instance.KokoroApi.EvadeHook.DefaultActionPaymentOption))
			return;
		
		EvadesLeft--;
	}

	public bool IsEvadePaymentOptionEnabled(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePaymentOptionEnabledArgs args)
	{
		if (EvadesLeft > 0)
			return true;
		if (Instance.Helper.Utilities.Unproxy(args.Entry) != Instance.Helper.Utilities.Unproxy(Instance.KokoroApi.EvadeHook.DefaultAction))
			return true;
		if (Instance.Helper.Utilities.Unproxy(args.PaymentOption) != Instance.Helper.Utilities.Unproxy(Instance.KokoroApi.EvadeHook.DefaultActionPaymentOption))
			return true;
		
		return false;
	}
}