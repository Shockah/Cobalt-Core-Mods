namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IActionInfoApi"/>
		IActionInfoApi ActionInfo { get; }

		/// <summary>
		/// Allows checking additional action information, like what card an action came from.
		/// </summary>
		public interface IActionInfoApi
		{
			/// <summary>
			/// Returns the ID of the card the given action came from.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The ID of the card the given action came from.</returns>
			int? GetSourceCardId(CardAction action);

			/// <summary>
			/// Returns the card the given action came from.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="action">The action.</param>
			/// <returns>The card the given action came from.</returns>
			Card? GetSourceCard(State state, CardAction action);

			/// <summary>
			/// Sets the ID of the card the given action came from.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <param name="sourceId">The ID of the card the given action came from.</param>
			void SetSourceCardId(CardAction action, int? sourceId);

			/// <summary>
			/// Sets the card the given action came from.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <param name="state">The game state.</param>
			/// <param name="source">The card the given action came from.</param>
			void SetSourceCard(CardAction action, State state, Card? source);
		}
	}
}
