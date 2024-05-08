using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class CardTooltipRenderer
{
	private RenderTarget2D? CurrentRenderTarget;

	public void Render(G g, bool withScreenFilter, Card card, bool withTheCard, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;
		var oldScreenLimits = Tooltips.SCREEN_LIMITS;
		var oldActivateAllActions = CardPatches.ActivateAllActions;

		g.mg.PIX_SCALE = 4;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);
		Tooltips.SCREEN_LIMITS = new(0, 0, double.PositiveInfinity, double.PositiveInfinity);
		CardPatches.ActivateAllActions = true;

		try
		{
			var cardTooltips = card.GetAllTooltips(g, DB.fakeState).ToList();

			var tooltips = new Tooltips();
			tooltips.Add(Vec.Zero, cardTooltips);
			tooltips.Render(g);

			if (withTheCard)
				Tooltips._tooltipScratch.Insert(0, new TTCard { card = card, showCardTraitTooltips = false });

			var margins = 6;
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

			if (CurrentRenderTarget is null || CurrentRenderTarget.Width != tooltipWidth * g.mg.PIX_SCALE || CurrentRenderTarget.Height != tooltipHeight * g.mg.PIX_SCALE)
			{
				CurrentRenderTarget?.Dispose();
				CurrentRenderTarget = new(g.mg.GraphicsDevice, tooltipWidth * g.mg.PIX_SCALE, tooltipHeight * g.mg.PIX_SCALE);
			}

			var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

			g.mg.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);

			g.mg.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
			Draw.StartAutoBatchFrame();
			try
			{
				Tooltip.RenderMultiple(g, tooltips.pos, Tooltips._tooltipScratch);
			}
			catch
			{
				ModEntry.Instance.Logger.LogError("There was an error exporting card {Card}.", card.Key());
			}
			if (withScreenFilter)
				Draw.Rect(0, 0, tooltipWidth * g.mg.PIX_SCALE, tooltipHeight * g.mg.PIX_SCALE, Colors.screenOverlay, new BlendState
				{
					ColorBlendFunction = BlendFunction.Add,
					ColorSourceBlend = Blend.One,
					ColorDestinationBlend = Blend.InverseSourceColor,
					AlphaSourceBlend = Blend.DestinationAlpha,
					AlphaDestinationBlend = Blend.DestinationAlpha
				});
			Draw.EndAutoBatchFrame();

			g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

			CurrentRenderTarget.SaveAsPng(stream, tooltipWidth * g.mg.PIX_SCALE, tooltipHeight * g.mg.PIX_SCALE);
		}
		finally
		{
			g.mg.PIX_SCALE = oldPixScale;
			g.mg.cameraMatrix = oldCameraMatrix;
			Tooltips.SCREEN_LIMITS = oldScreenLimits;
			CardPatches.ActivateAllActions = oldActivateAllActions;
		}
	}
}