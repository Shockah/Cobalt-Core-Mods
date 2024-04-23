using Nickel;
using System;

namespace Shockah.Dyna;

public sealed class ApiImplementation : IDynaApi
{
	public IDeckEntry DynaDeck
		=> ModEntry.Instance.DynaDeck;

	public IStatusEntry TempNitroStatus
		=> NitroManager.TempNitroStatus;

	public IStatusEntry NitroStatus
		=> NitroManager.NitroStatus;

	public IStatusEntry BastionStatus
		=> BastionManager.BastionStatus;

	public PDamMod FluxDamageModifier
		=> FluxPartModManager.FluxDamageModifier;

	public int GetBlastwaveDamage(Card? card, State state, int baseDamage, bool targetPlayer = false, int blastwaveIndex = 0)
	{
		foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.KokoroApi, state.EnumerateAllArtifacts()))
			baseDamage += hook.ModifyBlastwaveDamage(card, state, targetPlayer, blastwaveIndex);
		return Math.Max(baseDamage, 0);
	}

	public bool IsBlastwave(AAttack attack)
		=> attack.IsBlastwave();

	public bool IsStunwave(AAttack attack)
		=> attack.IsStunwave();

	public int? GetBlastwaveDamage(AAttack attack)
		=> attack.GetBlastwaveDamage();

	public int GetBlastwaveRange(AAttack attack)
		=> attack.GetBlastwaveRange();

	public AAttack SetBlastwave(AAttack attack, int? damage, int range = 1, bool isStunwave = false)
		=> attack.SetBlastwave(damage, range, isStunwave);

	public IDynaCharge MakeBurstCharge()
		=> new BurstCharge();

	public IDynaCharge MakeConcussionCharge()
		=> new ConcussionCharge();

	public IDynaCharge MakeDemoCharge()
		=> new DemoCharge();

	public IDynaCharge MakeFluxCharge()
		=> new FluxCharge();

	public IDynaCharge MakeShatterCharge()
		=> new ShatterCharge();

	public IDynaCharge MakeSwiftCharge()
		=> new SwiftCharge();

	public CardAction MakeFireChargeAction(IDynaCharge charge, int offset = 0, bool targetPlayer = false)
		=> new FireChargeAction
		{
			Charge = charge,
			Offset = offset,
			TargetPlayer = targetPlayer
		};

	public IDynaCharge? GetStickedCharge(State state, Combat combat, Part part)
		=> part.GetStickedCharge();

	public void SetStickedCharge(State state, Combat combat, Part part, IDynaCharge? charge)
		=> part.SetStickedCharge(charge);

	public bool TriggerChargeIfAny(State state, Combat combat, Part part, bool targetPlayer)
		=> ChargeManager.TriggerChargeIfAny(state, combat, part, targetPlayer);

	public void DefaultRenderChargeImplementation(IDynaCharge charge, G g, State state, Combat combat, Ship ship, int worldX, Vec position)
		=> ChargeManager.DefaultRenderChargeImplementation(charge, g, state, position);

	public void RegisterHook(IDynaHook hook, double priority)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IDynaHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}
