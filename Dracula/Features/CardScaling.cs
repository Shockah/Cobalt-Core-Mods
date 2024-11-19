using Microsoft.Xna.Framework;
using Shockah.Kokoro;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class CardScalingManager : IKokoroApi.IV2.ICardRenderingApi.IHook
{
	public CardScalingManager()
	{
		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(this);
	}

	public Matrix ModifyNonTextCardRenderMatrix(IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyNonTextCardRenderMatrixArgs args)
		=> (args.Card as IDraculaCard)?.ModifyNonTextCardRenderMatrix(args.G, args.Actions) ?? Matrix.Identity;

	public Matrix ModifyCardActionRenderMatrix(IKokoroApi.IV2.ICardRenderingApi.IHook.IModifyCardActionRenderMatrixArgs args)
		=> (args.Card as IDraculaCard)?.ModifyCardActionRenderMatrix(args.G, args.Actions, args.Action, args.ActionWidth) ?? Matrix.Identity;
}
