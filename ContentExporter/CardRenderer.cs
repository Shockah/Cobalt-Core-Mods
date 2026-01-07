using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Shockah.Shared;

namespace Shockah.ContentExporter;

internal sealed partial class Settings
{
	[JsonProperty]
	public int? CardsScale;
}

internal sealed class CardRenderer
{
	private static readonly Vec BaseCardSize = new(59, 82);
	private static readonly Vec OverborderCardSize = new(67, 90);

	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, Card card, Stream stream)
	{
		try
		{
			var imageSize = GetImageSize(card);
			
			using var texture = TextureUtils.CreateTexture(new(imageSize)
			{
				SkipTexture = true,
				Scale = scale,
				Actions = contentSize =>
				{
					switch (background)
					{
						case ExportBackground.Black:
							Draw.Rect(0, 0, contentSize.x, contentSize.y, Colors.black);
							break;
						case ExportBackground.White:
							Draw.Rect(0, 0, contentSize.x, contentSize.y, Colors.white);
							break;
					}

					card.Render(
						g,
						posOverride: new((imageSize.x - BaseCardSize.x) / 2 + 1, (imageSize.y - BaseCardSize.y) / 2 + 1),
						fakeState: DB.fakeState,
						ignoreAnim: true,
						ignoreHover: true
					);
			
					if (withScreenFilter)
						Draw.Rect(0, 0, contentSize.x, contentSize.y, Colors.screenOverlay, new BlendState
						{
							ColorBlendFunction = BlendFunction.Add,
							ColorSourceBlend = Blend.One,
							ColorDestinationBlend = Blend.InverseSourceColor,
							AlphaSourceBlend = Blend.Zero,
							AlphaDestinationBlend = Blend.One
						});
				},
			});
			
			texture.SaveAsPng(stream, texture.Width, texture.Height);
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("There was an error exporting card {Card} for deck {Deck}: {Exception}", card, card.GetMeta().deck.Key(), ex);
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