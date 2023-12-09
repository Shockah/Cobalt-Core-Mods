using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DizzyRiggsArtifact : DuoArtifact
{
	private static int ShieldChange = 0;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_Set_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_Set_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		ShieldChange = 0;
	}

	public override void OnCombatEnd(State state)
	{
		base.OnCombatEnd(state);
		ShieldChange = 0;
	}

	private static void Ship_Set_Prefix(Ship __instance, Status status, ref int __state)
		=> __state = __instance.Get(status);

	private static void Ship_Set_Postfix(Ship __instance, Status status, ref int __state)
	{
		if (status != Status.shield)
			return;
		int change = __instance.Get(Status.shield) - __state;
		ShieldChange += change;
	}

	private static void Combat_Update_Postfix(G g)
	{
		if (ShieldChange == 0)
			return;
		ShieldChange = 0;

		if (g.state.ship.Get(Status.shield) > 0)
			return;

		var artifact = g.state.EnumerateAllArtifacts().FirstOrDefault(a => a is DizzyRiggsArtifact);
		if (artifact is null)
			return;

		artifact.Pulse();
		(g.state.route as Combat)?.Queue(new AStatus
		{
			status = Status.evade,
			statusAmount = 1,
			targetPlayer = true
		});
	}
}