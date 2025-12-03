using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace Shockah.ContentExporter;

internal sealed class CardRenderer
{
	private static readonly Vec BaseCardSize = new(59, 82);
	private static readonly Vec OverborderCardSize = new(67, 90);

	private RenderTarget2D? CurrentRenderTarget;

	public void Render(G g, int scale, bool withScreenFilter, Card card, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;

		g.mg.PIX_SCALE = scale;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);

		try
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
			try
			{
				card.Render(g, posOverride: new((imageSize.x - BaseCardSize.x) / 2 + 1, (imageSize.y - BaseCardSize.y) / 2 + 1), fakeState: DB.fakeState, ignoreAnim: true, ignoreHover: true);
			}
			catch
			{
				ModEntry.Instance.Logger.LogError("There was an error exporting card {Card} {Upgrade}.", card.Key(), card.upgrade);
			}
			if (withScreenFilter)
				Draw.Rect(0, 0, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE), Colors.screenOverlay, new BlendState
				{
					ColorBlendFunction = BlendFunction.Add,
					ColorSourceBlend = Blend.One,
					ColorDestinationBlend = Blend.InverseSourceColor,
					AlphaSourceBlend = Blend.Zero,
					AlphaDestinationBlend = Blend.One
				});
			Draw.EndAutoBatchFrame();

			g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

			CurrentRenderTarget.SaveAsPng(stream, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
		}
		finally
		{
			g.mg.PIX_SCALE = oldPixScale;
			g.mg.cameraMatrix = oldCameraMatrix;
		}
	}

	private Vec GetImageSize(Card card)
	{
		var meta = card.GetMeta();
		if (meta.deck is Deck.corrupted or Deck.evilriggs)
			return OverborderCardSize;

		if (ModEntry.Instance.Helper.Content.Decks.LookupByDeck(meta.deck) is { } deckEntry && deckEntry.Configuration.OverBordersSprite is { } overBordersSprite)
		{
			var texture = SpriteLoader.Get(overBordersSprite);
			if (texture is not null)
				return new(texture.Width, texture.Height);
		}
		return BaseCardSize;
	}
}