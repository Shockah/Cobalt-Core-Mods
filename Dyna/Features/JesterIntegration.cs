using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal sealed class JesterIntegration
{
	public JesterIntegration()
	{
		ModEntry.Instance.Helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase == Nickel.ModLoadPhase.AfterDbInit)
				OnReady();
		};
	}

	private void OnReady()
	{
		if (ModEntry.Instance.Helper.ModRegistry.GetApi<IJesterApi>("rft.Jester") is not { } api)
			return;
		api.RegisterProvider(new BlastwaveProvider());
	}

	public sealed class BlastwaveProvider : IJesterApi.IProvider
	{
		public IList<IJesterApi.IEntry> GetEntries(IJesterApi.IJesterRequest request)
		{
			var existingShotCount = request.Entries.Count(e => e.Tags.Contains("shot"));

			List<IJesterApi.IEntry> entries = [];
			for (var damage = 1; damage <= 3; damage++)
				for (var blastwaveDamage = 1; blastwaveDamage <= 2; blastwaveDamage++)
					entries.Add(new Entry(damage, blastwaveDamage, BlastwaveRange: 1, existingShotCount));

			return entries
				.Where(e => e.GetCost() >= request.MinCost && e.GetCost() <= request.MaxCost)
				.ToList();
		}

		public sealed class Entry(
			int Damage,
			int BlastwaveDamage,
			int BlastwaveRange,
			int ExistingShotCount
		) : IJesterApi.IEntry
		{
			public ISet<string> Tags { get; } = new HashSet<string> { "offensive", "attack", "shot" };

			public int GetActionCount() => 1;

			public IList<CardAction> GetActions(State s, Combat c)
				=> [
					new AAttack
					{
						damage = Card.GetActualDamage(s, Damage)
					}
					.SetBlastwave(damage: BlastwaveDamage, range: BlastwaveRange)
				];

			public int GetCost()
				=> Damage * 10 + BlastwaveDamage * BlastwaveRange * 15 + ExistingShotCount * 5;

			public IJesterApi.IEntry? GetUpgradeA(IJesterApi.IJesterRequest request, out int cost)
			{
				var increaseBlastwaveDamage = request.Random.NextInt() % 2 == 0;
				var entry = new Entry(Damage + (increaseBlastwaveDamage ? 0 : 1), BlastwaveDamage + (increaseBlastwaveDamage ? 1 : 0), BlastwaveRange, ExistingShotCount);
				cost = entry.GetCost() - GetCost();
				return entry;
			}

			public IJesterApi.IEntry? GetUpgradeB(IJesterApi.IJesterRequest request, out int cost)
			{
				if (BlastwaveRange >= 2)
				{
					cost = 0;
					return null;
				}

				var entry = new Entry(Damage, BlastwaveDamage, BlastwaveRange + 1, ExistingShotCount);
				cost = entry.GetCost() - GetCost();
				return entry;
			}

			public void AfterSelection(IJesterApi.IJesterRequest request) { }
		}
	}
}
