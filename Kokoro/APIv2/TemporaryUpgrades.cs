using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
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

			public interface ISetTemporaryUpgradeAction : ICardAction
			{
				int CardId { get; set; }
				Upgrade? Upgrade { get; set; }

				ISetTemporaryUpgradeAction SetCardId(int value)
				{
					this.CardId = value;
					return this;
				}

				ISetTemporaryUpgradeAction SetUpgrade(Upgrade? value)
				{
					this.Upgrade = value;
					return this;
				}
			}

			ISetTemporaryUpgradeAction? AsSetTemporaryUpgradeAction(CardAction action);
			ISetTemporaryUpgradeAction MakeSetTemporaryUpgradeAction(int cardId, Upgrade? upgrade);
			
			public interface IChooseTemporaryUpgradeAction : ICardAction
			{
				int CardId { get; set; }

				IChooseTemporaryUpgradeAction SetCardId(int value)
				{
					this.CardId = value;
					return this;
				}
			}
			
			IChooseTemporaryUpgradeAction? AsChooseTemporaryUpgradeAction(CardAction action);
			IChooseTemporaryUpgradeAction MakeChooseTemporaryUpgradeAction(int cardId);
		}
	}
}
