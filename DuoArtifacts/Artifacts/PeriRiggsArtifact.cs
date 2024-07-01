using HarmonyLib;
using Nanoray.Pintail;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class PeriRiggsArtifact : DuoArtifact, IEvadeHook
{
	private const int EvadesPerTurn = 2;

	public int EvadesLeft = EvadesPerTurn;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(AccessTools.AllAssemblies().First(a => (a.GetName().Name ?? a.GetName().FullName) == "Kokoro").GetType("Shockah.Kokoro.VanillaEvadeHook"), nameof(IEvadeHook.IsEvadePossible)),
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

	void IEvadeHook.AfterEvade(State state, Combat combat, int direction, IEvadeHook hook)
	{
		if (hook is not IProxyObject.IWithProxyTargetInstanceProperty hookMarker)
			return;
		if (Instance.KokoroApi.VanillaEvadeHook is not IProxyObject.IWithProxyTargetInstanceProperty vanillaEvadeHookMarker)
			return;
		if (!ReferenceEquals(hookMarker.ProxyTargetInstance, vanillaEvadeHookMarker.ProxyTargetInstance))
			return;
		EvadesLeft--;
	}

	private static void VanillaEvadeHook_IsEvadePossible_Postfix(State state, ref bool? __result)
	{
		var artifact = state.EnumerateAllArtifacts().OfType<PeriRiggsArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		if (artifact.EvadesLeft <= 0)
			__result = null;
	}
}