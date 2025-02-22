using System;
using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

internal sealed class CatMaxArtifact : DuoArtifact
{
	private static readonly Lazy<List<Status>> PossibleStatuses = new(() =>
	{
		var results = new List<Status>();
		var set = new HashSet<Status>();

		foreach (var card in DB.releasedCards)
		{
			var meta = card.GetMeta();
			if (meta.dontOffer)
				continue;

			try
			{
				HandleUpgrade(Upgrade.None);
				foreach (var upgrade in meta.upgradesTo)
					HandleUpgrade(upgrade);
			}
			finally
			{
				card.upgrade = Upgrade.None;
			}
			
			void HandleUpgrade(Upgrade upgrade)
			{
				card.upgrade = upgrade;
				
				foreach (var baseAction in card.GetActions(DB.fakeState, DB.fakeCombat))
				{
					foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(baseAction))
					{
						if (wrappedAction is not AStatus statusAction)
							continue;
						if (!statusAction.targetPlayer)
							continue;
						if (!set.Add(statusAction.status))
							continue;

						if (statusAction.status is Status.shield or Status.tempShield)
							continue;
						if (!DB.statuses.TryGetValue(statusAction.status, out var statusDef))
							continue;
						if (!statusDef.isGood)
							continue;
					
						results.Add(statusAction.status);
					}
				}
			}
		}

		return results;
	});

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;

		combat.QueueImmediate(new AStatus
		{
			status = PossibleStatuses.Value[state.rngActions.NextInt() % PossibleStatuses.Value.Count],
			statusAmount = 1,
			targetPlayer = true
		});
	}
}