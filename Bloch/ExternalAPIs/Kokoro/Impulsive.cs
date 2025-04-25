using System;
using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IImpulsiveApi"/>
		[Obsolete($"Use `{nameof(Impulsive)}` instead.")]
		IImpulsiveApi Spontaneous { get; }
		
		/// <inheritdoc cref="IImpulsiveApi"/>
		IImpulsiveApi Impulsive { get; }

		/// <summary>
		/// Allows using impulsive <see cref="CardAction">card actions</see>.
		/// These actions only trigger when the card is drawn, or when a turn starts with it still in hand, and only once per turn.
		/// </summary>
		public interface IImpulsiveApi
		{
			/// <summary>
			/// The card trait that is being temporarily added to cards, which already triggered their impulsive actions this turn.
			/// </summary>
			/// <remarks>
			/// This trait is not meant to be added via <see cref="IModCards"/> methods - it is purely visual, and adding it will not change any behavior.
			/// </remarks>
			[Obsolete($"Use `{nameof(ImpulsiveTriggeredTrait)}` instead.")]
			ICardTraitEntry SpontaneousTriggeredTrait { get; }
			
			/// <summary>
			/// The card trait that is being temporarily added to cards, which already triggered their impulsive actions this turn.
			/// </summary>
			/// <remarks>
			/// This trait is not meant to be added via <see cref="IModCards"/> methods - it is purely visual, and adding it will not change any behavior.
			/// </remarks>
			ICardTraitEntry ImpulsiveTriggeredTrait { get; }
			
			/// <summary>
			/// Casts the action as an impulsive action, if it is one.
			/// </summary>
			/// <param name="action">The potential impulsive action.</param>
			/// <returns>The impulsive action, if the given action is one, or <c>null</c> otherwise.</returns>
			IImpulsiveAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new impulsive action, wrapping the provided action.
			/// </summary>
			/// <param name="action">The action to wrap.</param>
			/// <returns>The new impulsive action.</returns>
			IImpulsiveAction MakeAction(CardAction action);
			
			/// <summary>
			/// Represents an impulsive action.
			/// These actions only trigger when the card is drawn, or when a turn starts with it still in hand, and only once per turn.
			/// </summary>
			public interface IImpulsiveAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The actual action to run.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Whether to show the icon for the wrapper action.
				/// </summary>
				bool ShowImpulsiveIcon { get; set; }
				
				/// <summary>
				/// Whether to show the tooltip for the wrapper action.
				/// </summary>
				bool ShowImpulsiveTooltip { get; set; }

				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IImpulsiveAction SetAction(CardAction value);

				/// <summary>
				/// Sets <see cref="ShowImpulsiveIcon"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IImpulsiveAction SetShowImpulsiveIcon(bool value);

				/// <summary>
				/// Sets <see cref="ShowImpulsiveTooltip"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IImpulsiveAction SetShowImpulsiveTooltip(bool value);
			}
		}
	}
}