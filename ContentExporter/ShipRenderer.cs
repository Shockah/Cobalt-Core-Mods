using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.ContentExporter;

internal sealed class ShipRenderer
{
	private RenderTarget2D? RenderTarget;

	public void Render(G g, bool withScreenFilter, Ship ship, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;

		g.mg.PIX_SCALE = ModEntry.Instance.Settings.ShipScale;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);
		DrawPatches.ReplacementSprite = (StableSpr.effects_glow_512_gain15, ModEntry.Instance.PremultipliedGlowSprite.Sprite);

		try
		{
			if (RenderTarget is null || RenderTarget.Width != g.mg.PIX_W * g.mg.PIX_SCALE || RenderTarget.Height != g.mg.PIX_H * g.mg.PIX_SCALE)
			{
				RenderTarget?.Dispose();
				RenderTarget = new(g.mg.GraphicsDevice, g.mg.PIX_W * g.mg.PIX_SCALE, g.mg.PIX_H * g.mg.PIX_SCALE);
			}

			var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

			g.mg.GraphicsDevice.SetRenderTarget(RenderTarget);

			g.mg.GraphicsDevice.Clear(MGColor.Transparent);
			Draw.StartAutoBatchFrame();
			try
			{
				var shipRect = ship.GetShipRect();
				ship.DrawShip(g, new(g.mg.PIX_W / 2 - shipRect.w / 2, g.mg.PIX_H / 2 - shipRect.h / 2), Vec.Zero);
			}
			catch
			{
				ModEntry.Instance.Logger.LogError("There was an error exporting ship {Ship}.", ship.key);
			}
			if (withScreenFilter)
				Draw.Fill(Colors.screenOverlay, new BlendState
				{
					ColorBlendFunction = BlendFunction.Add,
					ColorSourceBlend = Blend.One,
					ColorDestinationBlend = Blend.InverseSourceColor,
					AlphaSourceBlend = Blend.Zero,
					AlphaDestinationBlend = Blend.One
				});
			Draw.EndAutoBatchFrame();

			g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

			var colors = new MGColor[RenderTarget.Width * RenderTarget.Height];
			RenderTarget.GetData(colors);

			var top = 0;
			while (top < RenderTarget.Height - 1)
			{
				if (Enumerable.Range(0, RenderTarget.Width).Any(x => colors[x + top * RenderTarget.Width].A != 0))
					break;
				top++;
			}
			
			var bottom = RenderTarget.Height - 1;
			while (bottom > 0)
			{
				if (Enumerable.Range(0, RenderTarget.Width).Any(x => colors[x + bottom * RenderTarget.Width].A != 0))
					break;
				bottom--;
			}

			var left = 0;
			while (left < RenderTarget.Width - 1)
			{
				if (Enumerable.Range(0, RenderTarget.Height).Any(y => colors[left + y * RenderTarget.Width].A != 0))
					break;
				left++;
			}

			var right = RenderTarget.Width - 1;
			while (right > 0)
			{
				if (Enumerable.Range(0, RenderTarget.Height).Any(y => colors[right + y * RenderTarget.Width].A != 0))
					break;
				right--;
			}

			var textureWidth = right - left + 1;
			var textureHeight = bottom - top + 1;
			var textureData = new MGColor[textureWidth * textureHeight];
			
			for (var y = 0; y < textureHeight; y++)
				for (var x = 0; x < textureWidth; x++)
					textureData[x + y * textureWidth] = colors[(x + left) + (y + top) * RenderTarget.Width];
			
			var texture = new Texture2D(g.mg.GraphicsDevice, textureWidth, textureHeight);
			texture.SetData(textureData);
			texture.SaveAsPng(stream, textureWidth, textureHeight);
		}
		finally
		{
			g.mg.PIX_SCALE = oldPixScale;
			g.mg.cameraMatrix = oldCameraMatrix;
			DrawPatches.ReplacementSprite = null;
		}
	}
}