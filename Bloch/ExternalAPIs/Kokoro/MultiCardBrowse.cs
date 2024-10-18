using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IMultiCardBrowseApi MultiCardBrowse { get; }
		
		public interface IMultiCardBrowseApi
		{
			IMultiCardBrowseRoute? AsRoute(CardBrowse route);
			IMultiCardBrowseRoute MakeRoute(Action<CardBrowse>? @delegate = null);
			
			IReadOnlyList<Card>? GetSelectedCards(CardAction action);

			ICustomAction MakeCustomAction(CardAction action, string title);
			
			public interface IMultiCardBrowseRoute
			{
				CardBrowse AsRoute { get; }
				
				IReadOnlyList<ICustomAction>? CustomActions { get; set; }
				int MinSelected { get; set; }
				int MaxSelected { get; set; }
				bool EnabledSorting { get; set; }
				bool BrowseActionIsOnlyForTitle { get; set; }
				IReadOnlyList<Card>? CardsOverride { get; set; }

				IMultiCardBrowseRoute SetCustomActions(IReadOnlyList<ICustomAction>? value);
				IMultiCardBrowseRoute SetMinSelected(int value);
				IMultiCardBrowseRoute SetMaxSelected(int value);
				IMultiCardBrowseRoute SetEnabledSorting(bool value);
				IMultiCardBrowseRoute SetBrowseActionIsOnlyForTitle(bool value);
				IMultiCardBrowseRoute SetCardsOverride(IReadOnlyList<Card>? value);
			}
			
			public interface ICustomAction
			{
				CardAction Action { get; set; }
				string Title { get; set; }
				int MinSelected { get; set; }
				int MaxSelected { get; set; }

				ICustomAction SetAction(CardAction value);
				ICustomAction SetTitle(string value);
				ICustomAction SetMinSelected(int value);
				ICustomAction SetMaxSelected(int value);
			}
		}
	}
}
