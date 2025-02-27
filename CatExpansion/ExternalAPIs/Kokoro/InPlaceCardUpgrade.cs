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
			
			public interface ICardUpgrade : IRoute<CardUpgrade>
			{
				/// <summary>
				/// Whether the upgrade should be done in place, without moving the card.
				/// </summary>
				bool IsInPlace { get; set; }

				/// <summary>
				/// Sets <see cref="IsInPlace"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardUpgrade SetIsInPlace(bool value);
			}
		}
	}
}
