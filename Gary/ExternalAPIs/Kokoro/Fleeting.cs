using System.Collections.Generic;
using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IFleetingApi"/>
		IFleetingApi Fleeting { get; }

		/// <summary>
		/// Allows working with and using the Fleeting card trait.
		/// Fleeting cards exhaust when they are still in hand when a turn ends.
		/// </summary>
		public interface IFleetingApi
		{
			/// <summary>
			/// The Fleeting card trait.
			/// </summary>
			ICardTraitEntry Trait { get; }
			
			/// <summary>
			/// The top layer of the trait's icon, if rendered separately.
			/// </summary>
			Spr TopIconLayer { get; }
			
			/// <summary>
			/// The bottom layer of the trait's icon, if rendered separately.
			/// </summary>
			Spr BottomIconLayer { get; }
			
			/// <summary>
			/// The combined trait icon, if not rendering layers separately.
			/// </summary>
			Spr CombinedIcon { get; }

			/// <summary>
			/// Allows modifying an <see cref="ACardSelect"/> action with Fleeting-related changes.
			/// </summary>
			/// <param name="action">The action to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardSelect ModifyCardSelect(ACardSelect action);
			
			/// <summary>
			/// Allows modifying a <see cref="CardBrowse"/> route with Fleeting-related changes.
			/// </summary>
			/// <param name="route">The route to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardBrowse ModifyCardBrowse(CardBrowse route);
			
			/// <summary>
			/// Registers a new hook related to the Fleeting card trait.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to the Fleeting card trait.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// An <see cref="ACardSelect"/> action wrapper, which allows modifying it with Fleeting-related changes.
			/// </summary>
			public interface ICardSelect : ICardAction<ACardSelect>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Fleeting trait.
				/// </summary>
				bool? FilterFleeting { get; set; }

				/// <summary>
				/// Sets <see cref="FilterFleeting"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardSelect SetFilterFleeting(bool? value);
			}
			
			/// <summary>
			/// A <see cref="CardBrowse"/> route wrapper, which allows modifying it with Fleeting-related changes.
			/// </summary>
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Fleeting trait.
				/// </summary>
				bool? FilterFleeting { get; set; }

				/// <summary>
				/// Sets <see cref="FilterFleeting"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardBrowse SetFilterFleeting(bool? value);
			}

			/// <summary>
			/// A hook related to the Fleeting card trait.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Allows controlling whether a card should actually exhaust when it has the Fleeting trait.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the card should exhaust, <c>false</c> if not, <c>null</c> if this hook does not care. Defaults to <c>null</c>.</returns>
				bool? ShouldExhaustViaFleeting(IShouldExhaustViaFleetingArgs args) => null;
				
				/// <summary>
				/// Called before cards are actually exhausted via Fleeting.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void BeforeExhaustViaFleeting(IBeforeExhaustViaFleetingArgs args) { }
				
				/// <summary>
				/// Called after cards are actually exhausted via Fleeting.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void OnExhaustViaFleeting(IOnExhaustViaFleetingArgs args) { }

				/// <summary>
				/// The arguments for the <see cref="ShouldExhaustViaFleeting"/> hook method.
				/// </summary>
				public interface IShouldExhaustViaFleetingArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The Fleeting card.
					/// </summary>
					Card Card { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="BeforeExhaustViaFleeting"/> hook method.
				/// </summary>
				public interface IBeforeExhaustViaFleetingArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The cards that will be exhausted.
					/// </summary>
					IReadOnlyList<Card> Cards { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="OnExhaustViaFleeting"/> hook method.
				/// </summary>
				public interface IOnExhaustViaFleetingArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
					
					/// <summary>
					/// The cards that will be exhausted.
					/// </summary>
					IReadOnlyList<Card> Cards { get; }
				}
			}
		}
	}
}
