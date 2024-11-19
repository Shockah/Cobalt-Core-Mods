using daisyowl.text;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ICardRenderingApi CardRendering { get; }

		public interface ICardRenderingApi
		{
			void RegisterHook(IHook hook, double priority = 0);
			void UnregisterHook(IHook hook);
			
			public interface IHook : IKokoroV2ApiHook
			{
				bool ShouldDisableCardRenderingTransformations(IShouldDisableCardRenderingTransformationsArgs args) => false;
				Font? ReplaceTextCardFont(IReplaceTextCardFontArgs args) => null;
				Vec ModifyTextCardScale(IModifyTextCardScaleArgs args) => Vec.One;
				Matrix ModifyNonTextCardRenderMatrix(IModifyNonTextCardRenderMatrixArgs args) => Matrix.Identity;
				Matrix ModifyCardActionRenderMatrix(IModifyCardActionRenderMatrixArgs args) => Matrix.Identity;
				
				public interface IShouldDisableCardRenderingTransformationsArgs
				{
					G G { get; }
					Card Card { get; }
				}
				
				public interface IReplaceTextCardFontArgs
				{
					G G { get; }
					Card Card { get; }
				}
				
				public interface IModifyTextCardScaleArgs
				{
					G G { get; }
					Card Card { get; }
				}
				
				public interface IModifyNonTextCardRenderMatrixArgs
				{
					G G { get; }
					Card Card { get; }
					List<CardAction> Actions { get; }
				}
				
				public interface IModifyCardActionRenderMatrixArgs
				{
					G G { get; }
					Card Card { get; }
					List<CardAction> Actions { get; }
					CardAction Action { get; }
					int ActionWidth { get; }
				}
			}
		}
	}
}
