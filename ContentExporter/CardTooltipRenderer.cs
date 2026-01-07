using System;
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
	public int? CardTooltipsScale;
}

internal sealed class CardTooltipRenderer
{
	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, Card card, bool withTheCard, Stream stream)
	{
		var oldScreenLimits = Tooltips.SCREEN_LIMITS;
		var oldActivateAllActions = CardPatches.ActivateAllActions;

		try
		{
			Tooltips.SCREEN_LIMITS = new(0, 0, double.PositiveInfinity, double.PositiveInfinity);
			CardPatches.ActivateAllActions = true;
			
			var cardTooltips = card.GetAllTooltips(g, DB.fakeState).ToList();

			var tooltips = new Tooltips();
			tooltips.Add(Vec.Zero, cardTooltips);
			tooltips.Render(g);

			if (withTheCard)
				Tooltips._tooltipScratch.Insert(0, new TTCard { card = card, showCardTraitTooltips = false });

			const int margins = 6;
			var tooltipWidth = 0;
			var tooltipHeight = 0;

			foreach (var tooltip in Tooltips._tooltipScratch)
			{
				var rect = tooltip.Render(g, dontDraw: true);
				tooltipHeight += (int)rect.h + 7;
				tooltipWidth = Math.Max(tooltipWidth, (int)rect.w);
			}

			tooltipWidth += margins * 2;
			tooltipHeight += margins * 2 - 5;

			using var texture = TextureUtils.CreateTexture(new(tooltipWidth, tooltipHeight)
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

					Tooltip.RenderMultiple(g, tooltips.pos, Tooltips._tooltipScratch);

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
		finally
		{
			Tooltips.SCREEN_LIMITS = oldScreenLimits;
			CardPatches.ActivateAllActions = oldActivateAllActions;
		}
	}
}