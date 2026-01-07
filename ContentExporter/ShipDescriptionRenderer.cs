using System;
using System.IO;
using daisyowl.text;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Shockah.Shared;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.ContentExporter;

internal sealed partial class Settings
{
	[JsonProperty]
	public int? ShipDescriptionsScale = DEFAULT_SCALE;
}

internal sealed class ShipDescriptionRenderer
{
	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, Ship ship, Stream stream)
	{
		var oldTime = g.state.time;
		
		try
		{
			g.state.time = 0;
			DrawPatches.ReplacementSprite = (StableSpr.effects_glow_512_gain15, ModEntry.Instance.PremultipliedGlowSprite.Sprite);
			
			using var texture = TextureUtils.CreateTexture(new(g.mg.PIX_W, g.mg.PIX_H)
			{
				SkipTexture = true,
				Scale = scale,
				Actions = contentSize =>
				{
					var shipRect = ship.GetShipRect();
					ship.DrawShip(g, new(g.mg.PIX_W / 2 - shipRect.w / 2, g.mg.PIX_H / 2 - shipRect.h / 2 - 8), Vec.Zero);
					ship.RenderShipUI(g, new(g.mg.PIX_W / 2 - shipRect.w / 2, g.mg.PIX_H / 2 - shipRect.h / 2 - 8), "content-exporter-ship-description", positionSelf: true, isPreview: false, partsAreFocusable: false);
					Draw.Text(ship.GetName(), g.mg.PIX_W / 2, g.mg.PIX_H / 2 + shipRect.h / 2, color: Colors.textBold, outline: Colors.black, align: TAlign.Center);
					Draw.Text(Loc.T("ship.{0}.desc".FF(ship.key)), g.mg.PIX_W / 2, g.mg.PIX_H / 2 + shipRect.h / 2 + 16, color: Colors.textMain, outline: Colors.black, maxWidth: 140, align: TAlign.Center);

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
			ModEntry.Instance.Logger.LogError("There was an error exporting ship {Ship}: {Exception}", ship.key, ex);
		}
		finally
		{
			g.state.time = oldTime;
			DrawPatches.ReplacementSprite = null;
		}
	}
}