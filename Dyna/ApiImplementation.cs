using Nickel;
using System;

namespace Shockah.Dyna;

public sealed class ApiImplementation : IDynaApi
{
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

	public void RegisterHook(IDynaHook hook, double priority)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IDynaHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}
