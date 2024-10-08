using Nickel;

namespace Shockah.Dracula;

public partial interface IKokoroApi
{
	ITemporaryUpgradesApi TemporaryUpgrades { get; }
	
	public interface ITemporaryUpgradesApi
	{
		ICardTraitEntry CardTrait { get; }
		
		Tooltip UpgradeTooltip { get; }
		Tooltip DowngradeTooltip { get; }
		Tooltip SidegradeTooltip { get; }

		Upgrade GetPermanentUpgrade(Card card);
		Upgrade? GetTemporaryUpgrade(Card card);
		void SetPermanentUpgrade(Card card, Upgrade upgrade);
		void SetTemporaryUpgrade(Card card, Upgrade? upgrade);

		CardAction MakeSetTemporaryUpgradeAction(int cardId, Upgrade? upgrade);
		CardAction MakeChooseTemporaryUpgradeAction(int cardId);
	}
}
