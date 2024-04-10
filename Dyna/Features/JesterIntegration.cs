using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

internal sealed class JesterIntegration
{
	public JesterIntegration()
	{
		ModEntry.Instance.Helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != Nickel.ModLoadPhase.AfterDbInit)
				return;
			if (ModEntry.Instance.Helper.ModRegistry.GetApi<IJesterApi>("rft.Jester") is not { } api)
				return;

			api.RegisterProvider(new BlastwaveProvider());
			api.RegisterProvider(new ChargeProvider());
		};
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

	public sealed class ChargeProvider : IJesterApi.IProvider
	{
		public IList<IJesterApi.IEntry> GetEntries(IJesterApi.IJesterRequest request)
		{
			var allowedOffsets = Enumerable.Range(-1, 3)
				.Except(request.OccupiedMidrow)
				.ToList();
			if (allowedOffsets.Count == 0)
				return [];

			List<IJesterApi.IEntry> entries = [];

			entries.Add(new SameChargeEntry { AllowedOffsets = allowedOffsets, Charge = new BurstCharge(), CostPerCharge = 25, MaxAtSameSpot = int.MaxValue, Tags = new HashSet<string> { "offensive", "dyna-charge" } });
			entries.Add(new SameChargeEntry { AllowedOffsets = allowedOffsets, Charge = new DemoCharge(), CostPerCharge = 25, Tags = new HashSet<string> { "offensive", "dyna-charge" } });
			entries.Add(new SameChargeEntry { AllowedOffsets = allowedOffsets, Charge = new FluxCharge(), CostPerCharge = 25, Tags = new HashSet<string> { "defensive", "dyna-charge" } });
			entries.Add(new SameChargeEntry { AllowedOffsets = allowedOffsets, Charge = new SwiftCharge(), CostPerCharge = 25, MaxAtSameSpot = int.MaxValue, Tags = new HashSet<string> { "utility", "dyna-charge" } });

			entries.Add(new SameChargeEntry { AllowedOffsets = allowedOffsets, Charge = new ConcussionCharge(), CostPerCharge = 70, Tags = new HashSet<string> { "defensive", "dyna-charge" } });
			entries.Add(new SameChargeEntry { AllowedOffsets = allowedOffsets, Charge = new ShatterCharge(), CostPerCharge = 70, Tags = new HashSet<string> { "offensive", "dyna-charge" } });

			if (!allowedOffsets.Contains(0))
				return entries
					.Where(e => e.GetCost() >= request.MinCost && e.GetCost() <= request.MaxCost)
					.ToList();

			entries.Add(new DifferentChargesEntry { BaseCharge = new BurstCharge(), ExtraCharge = new SwiftCharge(), BaseChargeCost = 25, ExtraChargeCost = 25, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "utility", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new DemoCharge(), ExtraCharge = new BurstCharge(), BaseChargeCost = 25, ExtraChargeCost = 25, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new DemoCharge(), ExtraCharge = new SwiftCharge(), BaseChargeCost = 25, ExtraChargeCost = 25, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "utility", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new FluxCharge(), ExtraCharge = new BurstCharge(), BaseChargeCost = 25, ExtraChargeCost = 25, BaseChargeTags = ["defensive", "dyna-charge"], ExtraChargeTags = ["offensive", "defensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new FluxCharge(), ExtraCharge = new SwiftCharge(), BaseChargeCost = 25, ExtraChargeCost = 25, BaseChargeTags = ["defensive", "dyna-charge"], ExtraChargeTags = ["defensive", "utility", "dyna-charge"] });

			entries.Add(new DifferentChargesEntry { BaseCharge = new ConcussionCharge(), ExtraCharge = new BurstCharge(), BaseChargeCost = 70, ExtraChargeCost = 25, BaseChargeTags = ["defensive", "dyna-charge"], ExtraChargeTags = ["offensive", "defensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new BurstCharge(), ExtraCharge = new ConcussionCharge(), BaseChargeCost = 25, ExtraChargeCost = 70, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "defensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new ShatterCharge(), ExtraCharge = new BurstCharge(), BaseChargeCost = 70, ExtraChargeCost = 25, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new BurstCharge(), ExtraCharge = new ShatterCharge(), BaseChargeCost = 25, ExtraChargeCost = 70, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "dyna-charge"] });

			entries.Add(new DifferentChargesEntry { BaseCharge = new ConcussionCharge(), ExtraCharge = new DemoCharge(), BaseChargeCost = 70, ExtraChargeCost = 25, BaseChargeTags = ["defensive", "dyna-charge"], ExtraChargeTags = ["offensive", "defensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new DemoCharge(), ExtraCharge = new ConcussionCharge(), BaseChargeCost = 25, ExtraChargeCost = 70, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "defensive", "dyna-charge"] });

			entries.Add(new DifferentChargesEntry { BaseCharge = new ConcussionCharge(), ExtraCharge = new FluxCharge(), BaseChargeCost = 70, ExtraChargeCost = 25, BaseChargeTags = ["defensive", "dyna-charge"], ExtraChargeTags = ["defensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new FluxCharge(), ExtraCharge = new ConcussionCharge(), BaseChargeCost = 25, ExtraChargeCost = 70, BaseChargeTags = ["defensive", "dyna-charge"], ExtraChargeTags = ["defensive", "dyna-charge"] });

			entries.Add(new DifferentChargesEntry { BaseCharge = new ConcussionCharge(), ExtraCharge = new ShatterCharge(), BaseChargeCost = 70, ExtraChargeCost = 70, BaseChargeTags = ["defensive", "dyna-charge"], ExtraChargeTags = ["offensive", "defensive", "dyna-charge"] });
			entries.Add(new DifferentChargesEntry { BaseCharge = new ShatterCharge(), ExtraCharge = new ConcussionCharge(), BaseChargeCost = 70, ExtraChargeCost = 70, BaseChargeTags = ["offensive", "dyna-charge"], ExtraChargeTags = ["offensive", "defensive", "dyna-charge"] });

			return entries
				.Where(e => e.GetCost() >= request.MinCost && e.GetCost() <= request.MaxCost)
				.ToList();
		}

		public sealed class SameChargeEntry : IJesterApi.IEntry
		{
			public required DynaCharge Charge;
			public required int CostPerCharge;
			public required IList<int> AllowedOffsets;
			public IList<int> Offsets = [0];
			public required ISet<string> Tags { get; init; }
			public int MaxAtSameSpot = 2;
			public bool WithBlastwave = false;

			public int GetActionCount()
				=> Offsets.Count + (WithBlastwave ? 1 : 0);

			public IList<CardAction> GetActions(State s, Combat c)
			{
				List<CardAction> actions = [];
				actions.AddRange(Offsets.Select(i => (CardAction)new FireChargeAction { Charge = Mutil.DeepCopy(Charge), Offset = i }));
				if (WithBlastwave)
					actions.Add(new AAttack { damage = Card.GetActualDamage(s, 0), }.SetBlastwave(damage: 0));
				return actions;
			}

			public int GetCost()
				=> CostPerCharge * Offsets.Count + (WithBlastwave ? 10 : 0);

			public IJesterApi.IEntry? GetUpgradeA(IJesterApi.IJesterRequest request, out int cost)
			{
				if (WithBlastwave || (Offsets.Count > 1 && Offsets.ToHashSet().Count <= 1))
				{
					cost = 0;
					return null;
				}

				var entry = Mutil.DeepCopy(this);
				entry.WithBlastwave = true;
				cost = entry.GetCost() - GetCost();
				return entry;
			}

			public IJesterApi.IEntry? GetUpgradeB(IJesterApi.IJesterRequest request, out int cost)
			{
				foreach (var offset in AllowedOffsets.Shuffle(request.Random))
				{
					if (Offsets.Count(o => o == offset) >= MaxAtSameSpot)
						continue;

					var entry = Mutil.DeepCopy(this);
					entry.Offsets.Add(offset);
					cost = entry.GetCost() - GetCost();
					return entry;
				}

				cost = 0;
				return null;
			}

			public void AfterSelection(IJesterApi.IJesterRequest request)
			{
				request.Blacklist.Add("dyna-charge");
			}
		}

		public sealed class DifferentChargesEntry : IJesterApi.IEntry
		{
			public required DynaCharge BaseCharge;
			public required DynaCharge ExtraCharge;
			public required int BaseChargeCost;
			public required int ExtraChargeCost;
			public bool UseExtraCharge;
			public required HashSet<string> BaseChargeTags;
			public required HashSet<string> ExtraChargeTags;

			public ISet<string> Tags
				=> UseExtraCharge ? ExtraChargeTags : BaseChargeTags;

			public int GetActionCount()
				=> UseExtraCharge ? 2 : 1;

			public IList<CardAction> GetActions(State s, Combat c)
			{
				List<CardAction> actions = [
					new FireChargeAction { Charge = Mutil.DeepCopy(BaseCharge) }
				];

				if (UseExtraCharge)
					actions.Add(new FireChargeAction { Charge = Mutil.DeepCopy(ExtraCharge) });

				return actions;
			}

			public int GetCost()
				=> BaseChargeCost + (UseExtraCharge ? ExtraChargeCost : 0);

			public IJesterApi.IEntry? GetUpgradeA(IJesterApi.IJesterRequest request, out int cost)
			{
				cost = 0;
				return null;
			}

			public IJesterApi.IEntry? GetUpgradeB(IJesterApi.IJesterRequest request, out int cost)
			{
				if (UseExtraCharge)
				{
					cost = 0;
					return null;
				}

				var entry = Mutil.DeepCopy(this);
				entry.UseExtraCharge = true;
				cost = entry.GetCost() - GetCost();
				return entry;
			}

			public void AfterSelection(IJesterApi.IJesterRequest request)
			{
				request.Blacklist.Add("dyna-charge");
			}
		}
	}
}
