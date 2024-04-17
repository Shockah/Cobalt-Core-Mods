using System;
using System.Collections.Generic;

namespace Shockah.Dyna;

public sealed class GainShieldOrTempShieldAction : CardAction
{
	public bool TargetPlayer;
	public int Amount;
	public string? ShieldArtifactPulse;
	public Status? ShieldStatusPulse;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		List<CardAction> actions = [];

		var targetShip = TargetPlayer ? s.ship : c.otherShip;
		var amountLeft = Amount;
		var missingShield = Math.Max(targetShip.GetMaxShield() - targetShip.Get(Status.shield), 0);

		if (amountLeft > 0 && missingShield > 0)
		{
			var toGain = Math.Min(amountLeft, missingShield);
			amountLeft -= toGain;
			actions.Add(new AStatus
			{
				targetPlayer = TargetPlayer,
				status = Status.shield,
				statusAmount = toGain,
				artifactPulse = ShieldArtifactPulse ?? artifactPulse,
				statusPulse = ShieldStatusPulse ?? statusPulse
			});
		}

		if (amountLeft > 0)
			actions.Add(new AStatus
			{
				targetPlayer = TargetPlayer,
				status = Status.tempShield,
				statusAmount = amountLeft,
				artifactPulse = artifactPulse,
				statusPulse = statusPulse
			});

		timer = 0;
		c.QueueImmediate(actions);
	}
}