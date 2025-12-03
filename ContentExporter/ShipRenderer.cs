using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.Shared;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.ContentExporter;

internal sealed class ShipRenderer
{
	private RenderTarget2D? RenderTarget;

	public void Render(G g, int scale, bool withScreenFilter, Ship ship, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;
		var oldTime = g.state.time;

		g.mg.PIX_SCALE = scale;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);
		g.state.time = 0;
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

			var croppedTexture = TextureUtils.CropToContent(RenderTarget);
			croppedTexture.SaveAsPng(stream, croppedTexture.Width, croppedTexture.Height);
		}
		finally
		{
			g.mg.PIX_SCALE = oldPixScale;
			g.mg.cameraMatrix = oldCameraMatrix;
			g.state.time = oldTime;
			DrawPatches.ReplacementSprite = null;
		}
	}
}