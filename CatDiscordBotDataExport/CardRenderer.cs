using CobaltCoreModding.Definitions.ExternalItems;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class CardRenderer
{
	private static readonly Vec BaseCardSize = new(59, 82);
	private static readonly Vec OverborderCardSize = new(67, 90);

	private RenderTarget2D? CurrentRenderTarget;
	private readonly Dictionary<Deck, ExternalDeck> RecordedExternalDecks = new();

	public void Render(G g, Card card, Stream stream)
	{
		var imageSize = GetImageSize(card);
		if (CurrentRenderTarget is null || CurrentRenderTarget.Width != (int)(imageSize.x * g.mg.PIX_SCALE) || CurrentRenderTarget.Height != (int)(imageSize.y * g.mg.PIX_SCALE))
		{
			CurrentRenderTarget?.Dispose();
			CurrentRenderTarget = new(g.mg.GraphicsDevice, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
		}

		var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

		g.mg.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);

		g.mg.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
		Draw.StartAutoBatchFrame();
		card.Render(g, posOverride: new((imageSize.x - BaseCardSize.x) / 2 + 1, (imageSize.y - BaseCardSize.y) / 2 + 1), fakeState: DB.fakeState, ignoreAnim: true, ignoreHover: true);
		Draw.EndAutoBatchFrame();

		g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

		CurrentRenderTarget.SaveAsPng(stream, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
	}

	internal void RecordExternalDeck(ExternalDeck deck)
	{
		if (deck.Id is null)
			return;
		RecordedExternalDecks[(Deck)deck.Id.Value] = deck;
	}

	private Vec GetImageSize(Card card)
	{
		var meta = card.GetMeta();
		if (meta.deck is Deck.corrupted or Deck.evilriggs)
			return OverborderCardSize;
		if (RecordedExternalDecks.TryGetValue(meta.deck, out var externalDeck) && externalDeck.BordersOverSprite is { } bordersOverSprite && bordersOverSprite.Id is not null)
		{
			var texture = SpriteLoader.Get((Spr)bordersOverSprite.Id.Value);
			if (texture is not null)
				return new(texture.Width, texture.Height);
		}
		return BaseCardSize;
	}
}