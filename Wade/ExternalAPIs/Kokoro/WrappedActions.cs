using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IWrappedActionsApi"/>
		IWrappedActionsApi WrappedActions { get; }

		/// <summary>
		/// Allows working with wrapper/wrapped actions.
		/// </summary>
		/// <para>
		/// Wrapper actions are actions, which hold other actions.
		/// These actions include <see cref="IV2.Conditional">conditional actions</see>,
		/// <see cref="IV2.HiddenActions">hidden actions</see>, <see cref="IV2.SpoofedActions">spoofed actions</see>,
		/// <see cref="IV2.OnDiscard">on discard actions</see>, <see cref="IV2.OnTurnEnd">on turn end actions</see>, and more.
		/// </para>
		/// <para>
		/// It is necessary to use this API whenever you want to check if a given action is a specific type, or modify such actions (for example an <see cref="AAttack">attack</see>).
		/// </para>
		public interface IWrappedActionsApi
		{
			/// <summary>
			/// Registers a new hook related to wrapped actions.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c></param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to wrapped actions.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);

			/// <summary>
			/// Returns an enumerable of all actions wrapped within the given action.
			/// </summary>
			/// <param name="action">The potential wrapper action.</param>
			/// <returns>An enumerable of all actions wrapped within the given action, or <c>null</c> if it is not a wrapper action.</returns>
			IEnumerable<CardAction>? GetWrappedCardActions(CardAction action);
			
			/// <summary>
			/// Returns a flattened enumerable of all actions recursively wrapped within the given action.
			/// </summary>
			/// <param name="action">The potential wrapper action.</param>
			/// <returns>A flattened enumerable of all actions recursively wrapped within the given action, or an enumerable of just the given action.</returns>
			IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action);
			
			/// <summary>
			/// Returns a flattened enumerable of all actions recursively wrapped within the given action, optionally including the wrapper actions themselves.
			/// </summary>
			/// <param name="action">The potential wrapper action.</param>
			/// <param name="includingWrapperActions">Whether to also include the wrapper actions themselves.</param>
			/// <returns>A flattened enumerable of all actions recursively wrapped within the given action, or an enumerable of just the given action.</returns>
			IEnumerable<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions);
			
			/// <summary>
			/// A hook related to wrapped actions.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Provides wrapped actions for the given potential wrapper action.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>An enumerable of wrapped actions for the given potential wrapper action, or <c>null</c> if the action is not a wrapper action or this hook cannot handle it.</returns>
				IEnumerable<CardAction>? GetWrappedCardActions(IGetWrappedCardActionsArgs args);
				
				/// <summary>
				/// The arguments for the <see cref="GetWrappedCardActions"/> hook method.
				/// </summary>
				public interface IGetWrappedCardActionsArgs
				{
					/// <summary>
					/// The potential wrapper action being checked for any wrapped actions.
					/// </summary>
					CardAction Action { get; }
				}
			}
		}
	}
}
