using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nickel;

namespace Shockah.DuoArtifacts;

internal sealed class BooksPeriArtifact : DuoArtifact
{
	private const int AttackBuff = 1;
	private const int ShardThreshold = 3;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix))
		);
	}

	private static void Combat_TryPlayCard_Prefix(State s, Card card)
	{
		var key = $"{nameof(BooksPeriArtifact)}::AttackBuff";
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksPeriArtifact) is not { } artifact)
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(card, key);
			return;
		}

		var shard = s.ship.Get(Status.shard);
		if (s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			shard += s.ship.Get(Status.shield);

		if (shard < ShardThreshold)
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(card, key);
			return;
		}
		
		ModEntry.Instance.Helper.ModData.SetModData(card, key, AttackBuff);
		artifact.Pulse();
	}

	public override int ModifyBaseDamage(int baseDamage, Card? card, State state, Combat? combat, bool fromPlayer)
	{
		if (card is null)
			return 0;
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(card, $"{nameof(BooksPeriArtifact)}::AttackBuff") is not { } attackBuff)
			return 0;
		return attackBuff;
	}
}