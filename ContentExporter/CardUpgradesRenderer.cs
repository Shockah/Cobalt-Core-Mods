using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.Shared;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.ContentExporter;

internal sealed class CardUpgradesRenderer
{
	private RenderTarget2D? RenderTarget;

	public void Render(G g, bool withScreenFilter, Card card, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;
		var oldTitleString = DB.currentLocale.strings.GetValueOrDefault("cardUpgrade.titlePreview");

		g.mg.PIX_SCALE = ModEntry.Instance.Settings.CardScale;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);
		DB.currentLocale.strings["cardUpgrade.titlePreview"] = "";
		SharedArtPatches.DisableDrawing = true;

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
			}
			catch
			{
				ModEntry.Instance.Logger.LogError("There was an error exporting card {Card}.", card.Key());
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
			SharedArtPatches.DisableDrawing = false;

			if (oldTitleString is null)
				DB.currentLocale.strings.Remove("cardUpgrade.titlePreview");
			else
				DB.currentLocale.strings["cardUpgrade.titlePreview"] = oldTitleString;
		}
	}
}