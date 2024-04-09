using System.Collections.Generic;

namespace Shockah.Dyna;

public partial interface IJesterApi
{
	public void RegisterProvider(IProvider provider);

	public interface IProvider
	{
		public IList<IEntry> GetEntries(IJesterRequest request);
	}

	public interface IEntry
	{
		public ISet<string> Tags { get; }

		public int GetActionCount();

		public IList<CardAction> GetActions(State s, Combat c);

		public int GetCost();

		public IEntry? GetUpgradeA(IJesterRequest request, out int cost);

		public IEntry? GetUpgradeB(IJesterRequest request, out int cost);

		public void AfterSelection(IJesterRequest request);
	}

	public interface IJesterRequest
	{
		// provided by caller
		public int Seed { get; set; }
		public string? FirstAction { get; set; }
		public State State { get; set; }
		public int BasePoints { get; set; }
		public CardData CardData { get; set; }
		public int ActionLimit { get; set; }
		public bool SingleUse { get; set; }
		public CardMeta CardMeta { get; set; }

		// calculation
		public Rand Random { get; set; }
		public IList<IEntry> Entries { get; set; }
		public ISet<string> Blacklist { get; set; }
		public ISet<string> Whitelist { get; set; }
		public ISet<int> OccupiedMidrow { get; set; }
		public int MinCost { get; set; }
		public int MaxCost { get; set; }
		public IDictionary<string, object> Data { get; set; } // misc data for your magical needs
	}
}