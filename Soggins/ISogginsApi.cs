using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Soggins;

public interface ISogginsApi
{
	Tooltip FrogproofCardTraitTooltip { get; }

	ExternalStatus FrogproofingStatus { get; }
	Tooltip FrogproofingTooltip { get; }

	ExternalStatus SmugStatus { get; }
	Tooltip GetSmugTooltip();
	Tooltip GetSmugTooltip(State state, Ship ship);
	int GetMinSmug(Ship ship);
	int GetMaxSmug(Ship ship);
	int? GetSmug(Ship ship);
	bool IsOversmug(Ship ship);
	double GetSmugBotchChance(State state, Ship ship, Card? card);
	double GetSmugDoubleChance(State state, Ship ship, Card? card);
	void RegisterSmugHook(ISmugHook hook, double priority);
	void UnregisterSmugHook(ISmugHook hook);

	int GetTimesBotchedThisCombat(State state, Combat combat);

	bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context);
	void RegisterFrogproofHook(IFrogproofHook hook, double priority);
	void UnregisterFrogproofHook(IFrogproofHook hook);
}

public interface ISmugHook
{
	void OnCardBotchedBySmug(State state, Combat combat, Card card) { }
	void OnCardDoubledBySmug(State state, Combat combat, Card card) { }

	double ModifySmugBotchChance(State state, Ship ship, Card? card, double chance) => chance;
	double ModifySmugDoubleChance(State state, Ship ship, Card? card, double chance) => chance;
	int ModifyApologyAmountForBotchingBySmug(State state, Combat combat, Card card, int amount) => amount;
}

public enum FrogproofHookContext
{
	Rendering, Action
}

public enum FrogproofType
{
	None, Innate, Paid
}

public interface IFrogproofHook
{
	FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context);
	void PayForFrogproof(State state, Combat? combat, Card card);
}