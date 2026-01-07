using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Shockah.Shared;

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

	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, List<List<Card?>> cardGroups, Stream stream)
	{
		var anyCard = cardGroups.SelectMany(group => group).OfType<Card>().First();
		
		try
		{
			var rows = cardGroups.Count;
			var columns = cardGroups.Max(g => g.Count);
			var singleCardImageSize = GetImageSize(anyCard);
			var fullImageSize = new Vec(
				singleCardImageSize.x * columns + ModEntry.Instance.Settings.CharacterDeckColumnSpacing * (columns - 1),
				singleCardImageSize.y * rows + ModEntry.Instance.Settings.CharacterDeckRowSpacing * (rows - 1)
			);

			using var texture = TextureUtils.CreateTexture(new(fullImageSize)
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
			ModEntry.Instance.Logger.LogError("There was an error exporting cards for deck {Deck}: {Exception}", anyCard.GetMeta().deck.Key(), ex);
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