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
				bool ShouldDisableCardRenderingTransformations(G g, Card card) => false;
				Font? ReplaceTextCardFont(G g, Card card) => null;
				Vec ModifyTextCardScale(G g, Card card) => Vec.One;
				Matrix ModifyNonTextCardRenderMatrix(G g, Card card, List<CardAction> actions) => Matrix.Identity;
				Matrix ModifyCardActionRenderMatrix(G g, Card card, List<CardAction> actions, CardAction action, int actionWidth) => Matrix.Identity;
			}
		}
	}
}
