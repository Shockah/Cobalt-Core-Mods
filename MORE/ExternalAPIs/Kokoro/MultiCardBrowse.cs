using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IMultiCardBrowseApi"/>
		IMultiCardBrowseApi MultiCardBrowse { get; }
		
		/// <summary>
		/// Allows usage of an advanced <see cref="CardBrowse"/> that allows choosing multiple cards.
		/// </summary>
		public interface IMultiCardBrowseApi
		{
			/// <summary>
			/// Casts the route to <see cref="IMultiCardBrowseRoute"/>, if it is one.
			/// </summary>
			/// <param name="route">The route.</param>
			/// <returns>The <see cref="IMultiCardBrowseRoute"/>, if the given route is one, or <c>null</c> otherwise.</returns>
			IMultiCardBrowseRoute? AsRoute(CardBrowse route);
			
			/// <summary>
			/// Creates a new <see cref="IMultiCardBrowseRoute"/> out of the given <see cref="CardBrowse"/>.
			/// </summary>
			/// <param name="route">The route to build upon.</param>
			/// <returns>THe new route.</returns>
			IMultiCardBrowseRoute MakeRoute(CardBrowse route);
			
			/// <summary>
			/// Retrieves the list of cards selected with a <see cref="IMultiCardBrowseRoute"/>.
			/// </summary>
			/// <param name="action">The action the cards were set on. See <see cref="CardBrowse.browseAction"/>.</param>
			/// <returns>The list of selected cards, or <c>null</c> if the action was not used with a <see cref="IMultiCardBrowseRoute"/> or choice was cancelled.</returns>
			IReadOnlyList<Card>? GetSelectedCards(CardAction action);
			
			/// <summary>
			/// Overrides the list of cards selected with a <see cref="IMultiCardBrowseRoute"/>.
			/// </summary>
			/// <param name="action">The action to set the cards on. See <see cref="CardBrowse.browseAction"/>.</param>
			/// <param name="cards">The list of selected cards, or <c>null</c> if the action was not used with a <see cref="IMultiCardBrowseRoute"/> or choice was cancelled.</param>
			void SetSelectedCards(CardAction action, IEnumerable<Card>? cards);

			/// <summary>
			/// Creates a custom action for a <see cref="IMultiCardBrowseRoute"/>, shown as a button.
			/// </summary>
			/// <param name="action">The action to run. The action can check the selected cards with <see cref="GetSelectedCards"/>.</param>
			/// <param name="title">The title displayed on the button.</param>
			/// <returns></returns>
			ICustomAction MakeCustomAction(CardAction action, string title);

			/// <summary>
			/// An advanced <see cref="CardBrowse"/> route that allows choosing multiple cards.
			/// </summary>
			public interface IMultiCardBrowseRoute : IRoute<CardBrowse>
			{
				/// <summary>
				/// Custom actions shown as buttons.
				/// </summary>
				IReadOnlyList<ICustomAction>? CustomActions { get; set; }
				
				/// <summary>
				/// The minimum amount of cards that can be selected. Defaults to <c>0</c>.
				/// </summary>
				int MinSelected { get; set; }
				
				/// <summary>
				/// The maximum amount of cards that can be selected. Defaults to <see cref="int.MaxValue"/>.
				/// </summary>
				int MaxSelected { get; set; }
				
				/// <summary>
				/// Whether card sorting modes should be visible and cards sorted. If <c>false</c>, cards will be displayed in the provided order. Defaults to <c>true</c>.
				/// </summary>
				bool EnabledSorting { get; set; }
				
				/// <summary>
				/// If set to <c>true</c> and there are other <see cref="CustomActions">custom actions</see>, the <see cref="CardBrowse.browseAction">main browse action</see> will only be used to provide the title for the screen.
				/// Otherwise, it will have its own "Done" button.
				/// </summary>
				bool BrowseActionIsOnlyForTitle { get; set; }
				
				/// <summary>
				/// The cards that should be shown. If not set, the cards will be selected automatically according to the <see cref="CardBrowse.browseSource"/>.
				/// </summary>
				IReadOnlyList<Card>? CardsOverride { get; set; }

				/// <summary>
				/// Sets <see cref="CustomActions"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiCardBrowseRoute SetCustomActions(IReadOnlyList<ICustomAction>? value);
				
				/// <summary>
				/// Sets <see cref="MinSelected"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiCardBrowseRoute SetMinSelected(int value);
				
				/// <summary>
				/// Sets <see cref="MaxSelected"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiCardBrowseRoute SetMaxSelected(int value);
				
				/// <summary>
				/// Sets <see cref="EnabledSorting"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiCardBrowseRoute SetEnabledSorting(bool value);
				
				/// <summary>
				/// Sets <see cref="BrowseActionIsOnlyForTitle"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiCardBrowseRoute SetBrowseActionIsOnlyForTitle(bool value);
				
				/// <summary>
				/// Sets <see cref="CardsOverride"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiCardBrowseRoute SetCardsOverride(IReadOnlyList<Card>? value);
			}
			
			/// <summary>
			/// A custom action for a <see cref="IMultiCardBrowseRoute"/>, shown as a button.
			/// </summary>
			public interface ICustomAction
			{
				/// <summary>
				/// The action to run. The action can check the selected cards with <see cref="GetSelectedCards"/>.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// The title displayed on the button.
				/// </summary>
				string Title { get; set; }
				
				/// <summary>
				/// An override for the minimum amount of cards that can be selected.
				/// </summary>
				int? MinSelected { get; set; }
				
				/// <summary>
				/// An override for the maximum amount of cards that can be selected.
				/// </summary>
				int? MaxSelected { get; set; }

				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICustomAction SetAction(CardAction value);
				
				/// <summary>
				/// Sets <see cref="Title"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICustomAction SetTitle(string value);
				
				/// <summary>
				/// Sets <see cref="MinSelected"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICustomAction SetMinSelected(int? value);
				
				/// <summary>
				/// Sets <see cref="MaxSelected"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICustomAction SetMaxSelected(int? value);
			}
		}
	}
}
