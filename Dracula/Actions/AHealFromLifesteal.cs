namespace Shockah.Dracula;

public sealed class AHealFromLifesteal : CardAction
{
	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var toHeal = c.otherShip.Get(ModEntry.Instance.LifestealStatus.Status);
		if (toHeal <= 0)
		{
			timer = 0;
			return;
		}

		c.QueueImmediate(new AHeal
		{
			targetPlayer = true,
			healAmount = toHeal
		});
		c.QueueImmediate(new AStatus
		{
			targetPlayer = false,
			mode = AStatusMode.Set,
			status = ModEntry.Instance.LifestealStatus.Status,
			statusAmount = 0
		});
	}
}