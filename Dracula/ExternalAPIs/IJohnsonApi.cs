using Nickel;

namespace Shockah.Dracula;

public interface IJohnsonApi
{
	IDeckEntry JohnsonDeck { get; }

	Tooltip TemporaryUpgradeTooltip { get; }
	bool IsTemporarilyUpgraded(Card card);
	void SetTemporarilyUpgraded(Card card, bool value);
}