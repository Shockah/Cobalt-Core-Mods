using daisyowl.text;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ICardRenderingApi"/>
		ICardRenderingApi CardRendering { get; }

		/// <summary>
		/// Allows modifying how a card is being rendered via a hook.
		/// </summary>
		public interface ICardRenderingApi
		{
			/// <summary>
			/// Registers a new hook related to card rendering.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c></param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to card rendering.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// A hook related to card rendering.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Controls whether any card rendering transformations should be enabled.
				/// </summary>
				/// <para>
				/// If any hook returns <c>false</c>, <see cref="ModifyTextCardScale"/>, <see cref="ModifyNonTextCardRenderMatrix"/> and <see cref="ModifyCardActionRenderMatrix"/> transformations will not take effect.
				/// </para>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if card rendering transformations should be disabled; <c>false</c> otherwise.</returns>
				bool ShouldDisableCardRenderingTransformations(IShouldDisableCardRenderingTransformationsArgs args) => false;
				
				/// <summary>
				/// Allows changing the font used for a card's text (not title).
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The replacement font to use, or <c>null</c> to use the default font.</returns>
				Font? ReplaceTextCardFont(IReplaceTextCardFontArgs args) => null;
				
				/// <summary>
				/// Allows modifying the scale of the card's text.
				/// </summary>
				/// <remarks>
				/// This hook method allows you to use non-pixel-perfect scales and should generally be avoided, and only used as a last resort.
				/// </remarks>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The scaling factor to use for the card's text. Defaults to <see cref="Vec.One"/>.</returns>
				Vec ModifyTextCardScale(IModifyTextCardScaleArgs args) => Vec.One;
				
				/// <summary>
				/// Allows transforming (for example, scaling) the whole block of a card's actions.
				/// </summary>
				/// <remarks>
				/// This hook method can be combined with <see cref="ModifyCardActionRenderMatrix"/> to produce more complicated transformations - for example decreasing the spacing between card actions.
				/// </remarks>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The matrix transformation to use for the whole block of a card's actions. Defaults to <see cref="Matrix.Identity"/>.</returns>
				Matrix ModifyNonTextCardRenderMatrix(IModifyNonTextCardRenderMatrixArgs args) => Matrix.Identity;
				
				/// <summary>
				/// Allows transforming (for example, scaling) a single action on a card.
				/// </summary>
				/// <remarks>
				/// This hook method can be combined with <see cref="ModifyNonTextCardRenderMatrix"/> to produce more complicated transformations - for example decreasing the spacing between card actions.
				/// </remarks>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>The matrix transformation to use for a single action on a card. Defaults to <see cref="Matrix.Identity"/>.</returns>
				Matrix ModifyCardActionRenderMatrix(IModifyCardActionRenderMatrixArgs args) => Matrix.Identity;
				
				/// <summary>
				/// The arguments for the <see cref="ShouldDisableCardRenderingTransformations"/> hook method.
				/// </summary>
				public interface IShouldDisableCardRenderingTransformationsArgs
				{
					/// <summary>
					/// The game instance.
					/// </summary>
					G G { get; }
					
					/// <summary>
					/// The card being rendered.
					/// </summary>
					Card Card { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="ReplaceTextCardFont"/> hook method.
				/// </summary>
				public interface IReplaceTextCardFontArgs
				{
					/// <summary>
					/// The game instance.
					/// </summary>
					G G { get; }
					
					/// <summary>
					/// The card being rendered.
					/// </summary>
					Card Card { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="ModifyTextCardScale"/> hook method.
				/// </summary>
				public interface IModifyTextCardScaleArgs
				{
					/// <summary>
					/// The game instance.
					/// </summary>
					G G { get; }
					
					/// <summary>
					/// The card being rendered.
					/// </summary>
					Card Card { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="ModifyNonTextCardRenderMatrix"/> hook method.
				/// </summary>
				public interface IModifyNonTextCardRenderMatrixArgs
				{
					/// <summary>
					/// The game instance.
					/// </summary>
					G G { get; }
					
					/// <summary>
					/// The card being rendered.
					/// </summary>
					Card Card { get; }
					
					/// <summary>
					/// The list of visible actions on the card.
					/// </summary>
					IReadOnlyList<CardAction> Actions { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="ModifyCardActionRenderMatrix"/> hook method.
				/// </summary>
				public interface IModifyCardActionRenderMatrixArgs
				{
					/// <summary>
					/// The game instance.
					/// </summary>
					G G { get; }
					
					/// <summary>
					/// The card being rendered.
					/// </summary>
					Card Card { get; }
					
					/// <summary>
					/// The list of visible actions on the card.
					/// </summary>
					IReadOnlyList<CardAction> Actions { get; }
					
					/// <summary>
					/// The action being rendered.
					/// </summary>
					CardAction Action { get; }
					
					/// <summary>
					/// The width of the action being rendered.
					/// </summary>
					int ActionWidth { get; }
				}
			}
		}
	}
}
