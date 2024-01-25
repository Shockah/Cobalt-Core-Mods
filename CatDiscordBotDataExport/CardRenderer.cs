using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class CardRenderer
{
	private static readonly Vec BaseCardSize = new(59, 82);
	private static readonly Vec OverborderCardSize = new(67, 90);

	private RenderTarget2D? CurrentRenderTarget;

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
		try
		{
			card.Render(g, posOverride: new((imageSize.x - BaseCardSize.x) / 2 + 1, (imageSize.y - BaseCardSize.y) / 2 + 1), fakeState: DB.fakeState, ignoreAnim: true, ignoreHover: true);
		}
		catch
		{
			ModEntry.Instance.Logger.LogError("There was an error exporting card {Card}.", card.Key());
		}
		Draw.EndAutoBatchFrame();

		g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

		CurrentRenderTarget.SaveAsPng(stream, (int)(imageSize.x * g.mg.PIX_SCALE), (int)(imageSize.y * g.mg.PIX_SCALE));
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