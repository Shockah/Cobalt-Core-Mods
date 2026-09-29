using System;
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
			/// A blank status info renderer. Only the border and the status icon will be rendered.
			/// </summary>
			IStatusInfoRenderer EmptyStatusInfoRenderer { get; }

			/// <summary>
			/// Creates a new text status info renderer.
			/// </summary>
			/// <param name="text">The text to display.</param>
			/// <returns>The new renderer.</returns>
			ITextStatusInfoRenderer MakeTextStatusInfoRenderer(string text);

			/// <summary>
			/// Casts the renderer to <see cref="ITextStatusInfoRenderer"/>, if it is one.
			/// </summary>
			/// <param name="renderer">The renderer.</param>
			/// <returns>The <see cref="ITextStatusInfoRenderer"/>, if the given renderer is one, or <c>null</c> otherwise.</returns>
			ITextStatusInfoRenderer? AsTextStatusInfoRenderer(IStatusInfoRenderer renderer);

			/// <summary>
			/// Creates a new bar status info renderer.
			/// The renderer needs to be configured before it displays anything.
			/// </summary>
			/// <returns>The new renderer.</returns>
			IBarStatusInfoRenderer MakeBarStatusInfoRenderer();

			/// <summary>
			/// Casts the renderer to <see cref="IBarStatusInfoRenderer"/>, if it is one.
			/// </summary>
			/// <param name="renderer">The renderer.</param>
			/// <returns>The <see cref="IBarStatusInfoRenderer"/>, if the given renderer is one, or <c>null</c> otherwise.</returns>
			IBarStatusInfoRenderer? AsBarStatusInfoRenderer(IStatusInfoRenderer renderer);

			/// <summary>
			/// Represents a type which renders info for a given status (shown on top of the ships in combat).
			/// </summary>
			public interface IStatusInfoRenderer
			{
				/// <summary>
				/// Render info for the given status.
				/// </summary>
				/// <param name="args">The arguments for this method.</param>
				/// <returns>The width of the info.</returns>
				int Render(IRenderArgs args);

				/// <summary>
				/// The arguments for the <see cref="Render"/> method.
				/// </summary>
				public interface IRenderArgs
				{
					/// <summary>
					/// The game instance.
					/// </summary>
					G G { get; }
					
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
					
					/// <summary>
					/// When <c>true</c>, no rendering should be done, only size calculations.
					/// </summary>
					bool DontRender { get; }
					
					/// <summary>
					/// The position at which to start rendering the info.
					/// </summary>
					Vec Position { get; }

					/// <summary>
					/// Creates a builder instance that can be used to edit existing arguments and pass them to child status info renderers.
					/// </summary>
					/// <returns>The new builder instance.</returns>
					IBuilder CopyToBuilder();

					public interface IBuilder : IRenderArgs
					{
						/// <summary>
						/// The current amount of the status.
						/// </summary>
						new int Amount { get; set; }
					
						/// <summary>
						/// The position at which to start rendering the info.
						/// </summary>
						new Vec Position { get; set; }
						
						/// <summary>
						/// Sets <see cref="Amount"/>.
						/// </summary>
						/// <param name="value">The new value.</param>
						/// <returns>This object after the change.</returns>
						IBuilder SetAmount(int value);
						
						/// <summary>
						/// Sets <see cref="Position"/>.
						/// </summary>
						/// <param name="value">The new value.</param>
						/// <returns>This object after the change.</returns>
						IBuilder SetPosition(Vec value);
					}
				}
			}
			
			/// <summary>
			/// A status info renderer that displays text.
			/// </summary>
			public interface ITextStatusInfoRenderer : IStatusInfoRenderer
			{
				/// <summary>
				/// The text to display.
				/// </summary>
				string Text { get; set; }
				
				/// <summary>
				/// The color of the text.
				/// </summary>
				Color Color { get; set; }

				/// <summary>
				/// Sets <see cref="Text"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITextStatusInfoRenderer SetText(string value);

				/// <summary>
				/// Sets <see cref="Color"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITextStatusInfoRenderer SetColor(Color value);
			}
			
			/// <summary>
			/// A status info renderer that displays a segmented bar, optionally in multiple rows.
			/// </summary>
			public interface IBarStatusInfoRenderer : IStatusInfoRenderer
			{
				/// <summary>
				/// The colors of the segments to display.
				/// </summary>
				IList<Color> Segments { get; set; }
				
				/// <summary>
				/// The width of a single segment.
				/// </summary>
				int SegmentWidth { get; set; }
				
				/// <summary>
				/// The spacing between segments, or columns of segments.
				/// </summary>
				int HorizontalSpacing { get; set; }
				
				/// <summary>
				/// The amount of rows the segments should be split into.
				/// </summary>
				/// <remarks>
				/// The allowed values are <c>1</c>, <c>2</c>, <c>3</c> and <c>5</c>.
				/// </remarks>
				int Rows { get; set; }
				
				/// <summary>
				/// Sets <see cref="Segments"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IBarStatusInfoRenderer SetSegments(IEnumerable<Color> value);
				
				/// <summary>
				/// Sets <see cref="SegmentWidth"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IBarStatusInfoRenderer SetSegmentWidth(int value);
				
				/// <summary>
				/// Sets <see cref="HorizontalSpacing"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IBarStatusInfoRenderer SetHorizontalSpacing(int value);
				
				/// <summary>
				/// Sets <see cref="Rows"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IBarStatusInfoRenderer SetRows(int value);
			}
			
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
				[Obsolete("Use `OverrideStatusInfoRenderer` instead.")]
				(IReadOnlyList<Color> Colors, int? BarSegmentWidth)? OverrideStatusRenderingAsBars(IOverrideStatusRenderingAsBarsArgs args) => null;
				
				/// <summary>
				/// Controls how status info (shown on top of the ships in combat) should be rendered.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The renderer to render the status info with, or <c>null</c> if this hook does not care.</returns>
				IStatusInfoRenderer? OverrideStatusInfoRenderer(IOverrideStatusInfoRendererArgs args) => null;
				
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
				/// The arguments for the <see cref="OverrideStatusInfoRenderer"/> hook method.
				/// </summary>
				public interface IOverrideStatusInfoRendererArgs
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
