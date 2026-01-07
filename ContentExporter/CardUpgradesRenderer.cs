using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Shockah.Shared;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.ContentExporter;

internal sealed partial class Settings
{
	[JsonProperty]
	public int? CardUpgradesScale;
}

internal sealed class CardUpgradesRenderer
{
	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, Card card, Stream stream)
	{
		var oldTitleString = DB.currentLocale.strings.GetValueOrDefault("cardUpgrade.titlePreview");

		try
		{
			DB.currentLocale.strings["cardUpgrade.titlePreview"] = "";
			SharedArtPatches.DisableDrawing = true;

			using var texture = TextureUtils.CreateTexture(new(g.mg.PIX_W, g.mg.PIX_H)
			{
				SkipTexture = true,
				Scale = scale,
				Actions = contentSize =>
				{
					card = card.CopyWithNewId();
					card.upgrade = Upgrade.None;
					card.drawAnim = 1;
				
					var route = new CardUpgrade
					{
						cardCopy = card,
						iHavePlayedThePoof = true,
						isPreview = true,
						isCodex = true,
					};
				
					var topCard = Mutil.DeepCopy(card);
					topCard.isForeground = false;
					topCard.hoverAnim = 0;
					route.topCard = topCard;
				
					for (var i = 0; i < route.particles.gradient.Count; i++)
						route.particles.gradient[i] = new Color(0f, 0f,  0f, 0f);

					route.Render(g);

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
			
			using var croppedTexture = TextureUtils.CropToContent(texture, background switch
			{
				ExportBackground.Black => withScreenFilter ? new MGColor(0xFF260306) : MGColor.Black,
				ExportBackground.White => MGColor.White,
				_ => null
			});
			croppedTexture.SaveAsPng(stream, croppedTexture.Width, croppedTexture.Height);
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("There was an error exporting card {Card} for deck {Deck}: {Exception}", card, card.GetMeta().deck.Key(), ex);
		}
		finally
		{
			SharedArtPatches.DisableDrawing = false;

			if (oldTitleString is null)
				DB.currentLocale.strings.Remove("cardUpgrade.titlePreview");
			else
				DB.currentLocale.strings["cardUpgrade.titlePreview"] = oldTitleString;
		}
	}
}