using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IStatusRenderingApi"/>
		IStatusRenderingApi StatusRendering { get; }

		/// <summary>
		/// Allows modifying how a status is being rendered via a hook.
		/// </summary>
		public interface IStatusRenderingApi
		{
			/// <summary>
			/// Registers a new hook related to status rendering.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c></param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to status rendering.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// The default color of an active status bar segment.
			/// </summary>
			Color DefaultActiveStatusBarColor { get; }
			
			/// <summary>
			/// The default color of an inactive status bar segment.
			/// </summary>
			Color DefaultInactiveStatusBarColor { get; }
			
			/// <summary>
			/// A hook related to status rendering.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Allows additionally showing statuses that normally would not be shown.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>An enumerable of the additional statuses to show. Defaults to an empty enumerable.</returns>
				IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(IGetExtraStatusesToShowArgs args) => [];
				
				/// <summary>
				/// Controls whether a status should be shown, if it were to be shown under normal circumstances.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the status should be shown, <c>false</c> if not, <c>null</c> if this hook does not care. Defaults to <c>null</c>.</returns>
				bool? ShouldShowStatus(IShouldShowStatusArgs args) => null;
				
				/// <summary>
				/// Controls whether a status should be rendered as a segmented bar (as opposed to a number).
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>
				/// The list of colors for the segments and an optional width for the segments. If the width is <c>null</c>, the default width is used.
				/// If the whole thing is <c>null</c>, the status will be displayed as a number.
				/// Defaults to <c>null</c>.
				/// </returns>
				(IReadOnlyList<Color> Colors, int? BarSegmentWidth)? OverrideStatusRenderingAsBars(IOverrideStatusRenderingAsBarsArgs args) => null;
				
				/// <summary>
				/// Allows changing the tooltips for a status.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The new list of tooltips. Defaults to <see cref="IOverrideStatusTooltipsArgs.Tooltips"/>.</returns>
				IReadOnlyList<Tooltip> OverrideStatusTooltips(IOverrideStatusTooltipsArgs args) => args.Tooltips;
				
				/// <summary>
				/// The arguments for the <see cref="GetExtraStatusesToShow"/> hook method.
				/// </summary>
				public interface IGetExtraStatusesToShowArgs
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
					/// The ship to show statuses for.
					/// </summary>
					Ship Ship { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="ShouldShowStatus"/> hook method.
				/// </summary>
				public interface IShouldShowStatusArgs
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
					/// The ship to show the status for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The current amount of the status.
					/// </summary>
					int Amount { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="OverrideStatusRenderingAsBars"/> hook method.
				/// </summary>
				public interface IOverrideStatusRenderingAsBarsArgs
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
					/// The ship to show the status for.
					/// </summary>
					Ship Ship { get; }
					
					/// <summary>
					/// The status.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The current amount of the status.
					/// </summary>
					int Amount { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="OverrideStatusTooltips"/> hook method.
				/// </summary>
				public interface IOverrideStatusTooltipsArgs
				{
					/// <summary>
					/// The status.
					/// </summary>
					Status Status { get; }
					
					/// <summary>
					/// The current amount of the status.
					/// </summary>
					int Amount { get; }
					
					/// <summary>
					/// The ship to get the status tooltips for, or <c>null</c> if it is not tied to a ship (for example, shown on a card).
					/// </summary>
					Ship? Ship { get; }
					
					/// <summary>
					/// The current tooltips.
					/// </summary>
					IReadOnlyList<Tooltip> Tooltips { get; }
				}
			}
		}
	}
}
