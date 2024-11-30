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
		if (args.Entry != Instance.KokoroApi.EvadeHook.DefaultAction)
			return;
		if (args.PaymentOption != Instance.KokoroApi.EvadeHook.DefaultActionPaymentOption)
			return;
		
		EvadesLeft--;
	}

	public bool IsEvadePaymentOptionEnabled(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePaymentOptionEnabledArgs args)
	{
		if (EvadesLeft > 0)
			return true;
		if (args.Entry != Instance.KokoroApi.EvadeHook.DefaultAction)
			return true;
		if (args.PaymentOption != Instance.KokoroApi.EvadeHook.DefaultActionPaymentOption)
			return true;
		return false;
	}
}