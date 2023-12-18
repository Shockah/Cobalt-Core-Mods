namespace Shockah.ABUpgrades;

internal static class PeriUpgrades
{
	public static void RegisterUpgrades(IApi api)
	{
		api.RegisterABUpgrade(
			typeof(SpareBattery),
			upgradeToCopyDataFrom: Upgrade.None,
			actions: (s, c, card) => new()
			{
				new AEnergy
				{
					changeAmount = 3
				},
				new AStatus
				{
					status = Status.energyNextTurn,
					statusAmount = 1,
					targetPlayer = true
				}
			}
		);
	}
}
