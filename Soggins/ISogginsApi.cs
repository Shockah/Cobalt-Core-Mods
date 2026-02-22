using CobaltCoreModding.Definitions.ExternalItems;
using Nickel;

namespace Shockah.Soggins;

public interface ISogginsApi
{
	ExternalDeck SogginsDeck { get; }
	Deck SogginsVanillaDeck { get; }

	ICardTraitEntry FrogproofTrait { get; }
	Tooltip FrogproofCardTraitTooltip { get; }

	ExternalStatus FrogproofingStatus { get; }
	Status FrogproofingVanillaStatus { get; }
	Tooltip FrogproofingTooltip { get; }

	ExternalStatus SmugStatus { get; }
	Status SmugVanillaStatus { get; }
	Tooltip GetSmugTooltip();
	Tooltip GetSmugTooltip(State state, Ship ship);
	bool IsRunWithSmug(State state);
	bool IsSmugEnabled(State state, Ship ship);
	void SetSmugEnabled(State state, Ship ship, bool enabled = true);
	int GetMinSmug(Ship ship);
	int GetMaxSmug(Ship ship);
	int? GetSmug(State state, Ship ship);
	bool IsOversmug(State state, Ship ship);
	double GetSmugBotchChance(State state, Ship ship, Card? card);
	double GetSmugDoubleChance(State state, Ship ship, Card? card);
	int GetTimesBotchedThisCombat(State state, Combat combat);
	SmugResult RollSmugResult(State state, Ship ship, Rand rng, Card? card);

	void RegisterSmugHook(ISmugHook hook, double priority);
	void UnregisterSmugHook(ISmugHook hook);

	Card GenerateAndTrackApology(State state, Combat combat, Rand rng);
	Card MakePlaceholderApology();

	bool IsFrogproof(State state, Combat? combat, Card card, FrogproofHookContext context);
	void RegisterFrogproofHook(IFrogproofHook hook, double priority);
	void UnregisterFrogproofHook(IFrogproofHook hook);
}

public enum SmugResult
{
	Botch = -1, Normal = 0, Double = 1
}

public interface ISmugHook
{
	void OnCardBotchedBySmug(State state, Combat combat, Card card) { }
	void OnCardDoubledBySmug(State state, Combat combat, Card card) { }

	double ModifySmugBotchChance(State state, Ship ship, Card? card, double chance) => chance;
	double ModifySmugDoubleChance(State state, Ship ship, Card? card, double chance) => chance;
	int ModifySmugSwing(State state, Combat combat, Card card, int amount) => amount;
	int ModifyApologyAmountForBotchingBySmug(State state, Combat combat, Card card, int amount) => amount;
}

public enum FrogproofHookContext
{
	Rendering, Action
}

public enum FrogproofType
{
	None, Innate, InnateHiddenIfNotNeeded, Paid
}

public interface IFrogproofHook
{
	FrogproofType? GetFrogproofType(State state, Combat? combat, Card card, FrogproofHookContext context);
	void PayForFrogproof(State state, Combat? combat, Card card);
}