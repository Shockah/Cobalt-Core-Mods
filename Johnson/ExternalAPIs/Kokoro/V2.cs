namespace Shockah.Kokoro;

/// <summary>
/// Allows accessing all of Kokoro library APIs.
/// </summary>
public partial interface IKokoroApi
{
	/// <inheritdoc cref="IV2"/>
	IV2 V2 { get; }
	
	/// <summary>
	/// Allows accessing Kokoro version 2 APIs. It is the recommended way of using Kokoro.
	/// </summary>
	public partial interface IV2
	{
		/// <summary>
		/// A Kokoro wrapper for a custom <see cref="CardAction"/>.
		/// </summary>
		/// <typeparam name="T">The more concrete type of the card action being wrapped.</typeparam>
		public interface ICardAction<out T> where T : CardAction
		{
			/// <summary>
			/// Returns the actual usable card action.
			/// </summary>
			T AsCardAction { get; }
		}
		
		/// <summary>
		/// A Kokoro wrapper for a custom <see cref="Route"/>.
		/// </summary>
		/// <typeparam name="T">The more concrete type of the route being wrapped.</typeparam>
		public interface IRoute<out T> where T : Route
		{
			/// <summary>
			/// Returns the actual usable route.
			/// </summary>
			T AsRoute { get; }
		}

		/// <summary>
		/// Marks a Kokoro version 2 API hook, as opposed to version 1 API hooks, which are not marked.
		/// </summary>
		public interface IKokoroV2ApiHook;
		
		/// <summary>
		/// Allows choosing the priority for an auto-implemented hook (for example, on <see cref="Artifact"/> types).
		/// </summary>
		public interface IHookPriority
		{
			/// <summary>
			/// The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.
			/// </summary>
			double HookPriority { get; }
		}
	}
}