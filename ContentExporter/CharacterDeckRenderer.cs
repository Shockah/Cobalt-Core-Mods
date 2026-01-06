using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Shockah.ContentExporter;

internal sealed partial class Settings
{
	[JsonProperty]
	public int? CharacterDeckScale = DEFAULT_SCALE;
	
	[JsonProperty]
	public int CharacterDeckColumnSpacing = 2;
	
	[JsonProperty]
	public int CharacterDeckRowSpacing = 2;
}

internal sealed class CharacterDeckRenderer
{
	private static readonly Vec BaseCardSize = new(59, 82);
	private static readonly Vec OverborderCardSize = new(67, 90);

	private RenderTarget2D? CurrentRenderTarget;

	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, List<List<Card?>> cardGroups, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;

		g.mg.PIX_SCALE = scale;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);

		try
		{
			var rows = cardGroups.Count;
			var columns = cardGroups.Max(g => g.Count);
			var anyCard = cardGroups.SelectMany(group => group).OfType<Card>().First();
			var singleCardImageSize = GetImageSize(anyCard);
			var fullImageSize = new Vec(
				singleCardImageSize.x * columns + ModEntry.Instance.Settings.CharacterDeckColumnSpacing * (columns - 1),
				singleCardImageSize.y * rows + ModEntry.Instance.Settings.CharacterDeckRowSpacing * (rows - 1)
			);
			if (CurrentRenderTarget is null || CurrentRenderTarget.Width != (int)(fullImageSize.x * g.mg.PIX_SCALE) || CurrentRenderTarget.Height != (int)(fullImageSize.y * g.mg.PIX_SCALE))
			{
				CurrentRenderTarget?.Dispose();
				CurrentRenderTarget = new(g.mg.GraphicsDevice, (int)(fullImageSize.x * g.mg.PIX_SCALE), (int)(fullImageSize.y * g.mg.PIX_SCALE));
			}

			var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

			g.mg.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);

			g.mg.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
			Draw.StartAutoBatchFrame();
			try
			{
				switch (background)
				{
					case ExportBackground.Black:
						Draw.Fill(Colors.black);
						break;
					case ExportBackground.White:
						Draw.Fill(Colors.white);
						break;
				}

				for (var rowIndex = 0; rowIndex < cardGroups.Count; rowIndex++)
				{
					var row = cardGroups[rowIndex];
					for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
					{
						var card = row[columnIndex];
						card?.Render(
							g,
							posOverride: new(
								(singleCardImageSize.x - BaseCardSize.x) / 2 + 1 + columnIndex * (singleCardImageSize.x + ModEntry.Instance.Settings.CharacterDeckColumnSpacing),
								(singleCardImageSize.y - BaseCardSize.y) / 2 + 1 + rowIndex * (singleCardImageSize.y + ModEntry.Instance.Settings.CharacterDeckRowSpacing)
							),
							fakeState: DB.fakeState,
							ignoreAnim: true,
							ignoreHover: true
						);
					}
				}
			}
			catch
			{
				ModEntry.Instance.Logger.LogError("There was an error exporting cards for deck {Deck}.", anyCard.GetMeta().deck.Key());
			}
			if (withScreenFilter)
				Draw.Rect(0, 0, (int)(fullImageSize.x * g.mg.PIX_SCALE), (int)(fullImageSize.y * g.mg.PIX_SCALE), Colors.screenOverlay, new BlendState
				{
					ColorBlendFunction = BlendFunction.Add,
					ColorSourceBlend = Blend.One,
					ColorDestinationBlend = Blend.InverseSourceColor,
					AlphaSourceBlend = Blend.Zero,
					AlphaDestinationBlend = Blend.One
				});
			Draw.EndAutoBatchFrame();

			g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

			CurrentRenderTarget.SaveAsPng(stream, (int)(fullImageSize.x * g.mg.PIX_SCALE), (int)(fullImageSize.y * g.mg.PIX_SCALE));
		}
		finally
		{
			g.mg.PIX_SCALE = oldPixScale;
			g.mg.cameraMatrix = oldCameraMatrix;
		}
	}

	private static Vec GetImageSize(Card card)
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