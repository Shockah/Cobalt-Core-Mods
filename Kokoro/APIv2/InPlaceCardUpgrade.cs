namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IInPlaceCardUpgradeApi"/>
		IInPlaceCardUpgradeApi InPlaceCardUpgrade { get; }
		
		/// <summary>
		/// Allows modifying a <see cref="CardUpgrade"/> to do the upgrade in place, without moving the card.
		/// </summary>
		public interface IInPlaceCardUpgradeApi
		{
			/// <summary>
			/// Allows modifying a <see cref="CardUpgrade"/> route to do the upgrade in place, without moving the card.
			/// </summary>
			/// <param name="route">The route.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardUpgrade ModifyCardUpgrade(CardUpgrade route);
			
			/// <summary>
			/// A <see cref="CardUpgrade"/> route wrapper, which allows modifying it to do the upgrade in place, without moving the card.
			/// </summary>
			public interface ICardUpgrade : IRoute<CardUpgrade>
			{
				/// <summary>
				/// Whether the upgrade should be done in place, without moving the card.
				/// </summary>
				bool IsInPlace { get; set; }
				
				/// <summary>
				/// A strategy that will be used to apply an upgrade to a card. Defaults to <c>null</c>, which will copy the value of the <see cref="Card.upgrade"/> field.
				/// </summary>
				IInPlaceCardUpgradeStrategy? InPlaceCardUpgradeStrategy { get; set; }

				/// <summary>
				/// Sets <see cref="IsInPlace"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardUpgrade SetIsInPlace(bool value);
				
				/// <summary>
				/// Sets <see cref="InPlaceCardUpgradeStrategy"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardUpgrade SetInPlaceCardUpgradeStrategy(IInPlaceCardUpgradeStrategy? value);
			}

			public interface IInPlaceCardUpgradeStrategy
			{
				void ApplyInPlaceCardUpgrade(IApplyInPlaceCardUpgradeArgs args);

				public interface IApplyInPlaceCardUpgradeArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The route that applies the upgrade to the card.
					/// </summary>
					CardUpgrade Route { get; }
					
					/// <summary>
					/// The card to apply the upgrade to.
					/// </summary>
					Card TargetCard { get; }
					
					/// <summary>
					/// The card chosen by the player as a template for the upgrade.
					/// </summary>
					Card TemplateCard { get; }
				}
			}
		}
	}
}
