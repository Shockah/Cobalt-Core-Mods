using HarmonyLib;
using Nanoray.Pintail;
using Nickel;
using Shockah.Kokoro;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class PeriRiggsArtifact : DuoArtifact, IKokoroApi.IV2.IEvadeHookApi.IHook
{
	private const int EvadesPerTurn = 2;

	public int EvadesLeft = EvadesPerTurn;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(AccessTools.AllAssemblies().First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro").GetType("Shockah.Kokoro.VanillaEvadeHook"), nameof(IKokoroApi.IV2.IEvadeHookApi.IHook.IsEvadePossible)),
			postfix: new HarmonyMethod(GetType(), nameof(VanillaEvadeHook_IsEvadePossible_Postfix))
		);
	}

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
		if (args.Hook is not IProxyObject.IWithProxyTargetInstanceProperty hookMarker)
			return;
		if (Instance.KokoroApi.EvadeHook.VanillaEvadeHook is not IProxyObject.IWithProxyTargetInstanceProperty vanillaEvadeHookMarker)
			return;
		if (!ReferenceEquals(hookMarker.ProxyTargetInstance, vanillaEvadeHookMarker.ProxyTargetInstance))
			return;
		EvadesLeft--;
	}

	private static void VanillaEvadeHook_IsEvadePossible_Postfix(object args, ref bool? __result)
	{
		if (!Instance.Helper.Utilities.ProxyManager.TryProxy<string, IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs>(args, "Shockah.Kokoro", Instance.Package.Manifest.UniqueName, out var typedArgs))
			return;
		if (typedArgs.State.EnumerateAllArtifacts().OfType<PeriRiggsArtifact>().FirstOrDefault() is not { } artifact)
			return;
		
		if (artifact.EvadesLeft <= 0)
			__result = null;
	}
}