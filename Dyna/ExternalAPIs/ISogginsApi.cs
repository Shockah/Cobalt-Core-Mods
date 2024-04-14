namespace Shockah.Dyna;

public interface ISogginsApi
{
	Deck SogginsVanillaDeck { get; }

	Tooltip GetSmugTooltip();

	void RegisterSmugHook(ISmugHook hook, double priority);
	void UnregisterSmugHook(ISmugHook hook);
}

public interface ISmugHook
{
	void OnCardBotchedBySmug(State state, Combat combat, Card card) { }
}