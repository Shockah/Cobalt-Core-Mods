using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ICustomCardBrowseSourceApi"/>
		ICustomCardBrowseSourceApi CustomCardBrowseSource { get; }
		
		/// <summary>
		/// Allows specifying custom card browse sources for <see cref="ACardSelect"/>/<see cref="CardBrowse"/>.
		/// </summary>
		public interface ICustomCardBrowseSourceApi
		{
			/// <summary>
			/// Allows modifying an <see cref="ACardSelect"/> action with a custom card browse source.
			/// </summary>
			/// <param name="action">The action to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardSelect ModifyCardSelect(ACardSelect action);
			
			/// <summary>
			/// Allows modifying a <see cref="CardBrowse"/> route with a custom card browse source.
			/// </summary>
			/// <param name="route">The route to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardBrowse ModifyCardBrowse(CardBrowse route);

			/// <summary>
			/// An <see cref="ACardSelect"/> action wrapper, which allows modifying it with a custom card browse source.
			/// </summary>
			public interface ICardSelect : ICardAction<ACardSelect>
			{
				/// <summary>
				/// The custom browse source that overrides one set via <see cref="ACardSelect.browseSource"/>.
				/// </summary>
				ICustomCardBrowseSource? CustomBrowseSource { get; set; }

				/// <summary>
				/// Sets <see cref="CustomBrowseSource"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardSelect SetCustomBrowseSource(ICustomCardBrowseSource? value);
			}
			
			/// <summary>
			/// A <see cref="CardBrowse"/> route wrapper, which allows modifying it with a custom card browse source.
			/// </summary>
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				/// <summary>
				/// The custom browse source that overrides one set via <see cref="CardBrowse.browseSource"/>.
				/// </summary>
				ICustomCardBrowseSource? CustomBrowseSource { get; set; }

				/// <summary>
				/// Sets <see cref="CustomBrowseSource"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardBrowse SetCustomBrowseSource(ICustomCardBrowseSource? value);
			}
			
			/// <summary>
			/// Represents a custom card browse source for <see cref="ACardSelect"/>/<see cref="CardBrowse"/>.
			/// </summary>
			public interface ICustomCardBrowseSource
			{
				/// <summary>
				/// Provides a list of tooltips for this custom card browse source.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <returns>The list of tooltips for this custom card browse source.</returns>
				IReadOnlyList<Tooltip> GetSearchTooltips(State state);
				
				/// <summary>
				/// Provides a list of cards in this browse source.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <returns>The list of cards in this browse source.</returns>
				IReadOnlyList<Card> GetCards(State state, Combat combat);
				
				/// <summary>
				/// Provides the title for a card browse screen.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <param name="cards">The cards in this browse source, returned via <see cref="GetCards"/>.</param>
				/// <returns>The title.</returns>
				string GetTitle(State state, Combat combat, IReadOnlyList<Card> cards);
			}
		}
	}
}
