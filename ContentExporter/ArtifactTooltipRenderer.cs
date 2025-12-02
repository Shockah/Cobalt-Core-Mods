using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nickel;
using System;
using System.IO;
using System.Linq;

namespace Shockah.ContentExporter;

internal sealed class ArtifactTooltipRenderer
{
	private readonly ISpriteEntry GlowSprite;
	private RenderTarget2D? CurrentRenderTarget;

	public ArtifactTooltipRenderer(ISpriteEntry glowSprite)
	{
		this.GlowSprite = glowSprite;
	}

	public void Render(G g, bool withScreenFilter, Artifact artifact, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;
		var oldMetaRoute = g.metaRoute;
		var oldScreenLimits = Tooltips.SCREEN_LIMITS;
		var oldActivateAllActions = CardPatches.ActivateAllActions;

		g.mg.PIX_SCALE = ModEntry.Instance.Settings.ArtifactScale;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);
		g.metaRoute = new MainMenu { subRoute = new Codex { subRoute = new ArtifactBrowse() } };
		Tooltips.SCREEN_LIMITS = new(0, 0, double.PositiveInfinity, double.PositiveInfinity);
		CardPatches.ActivateAllActions = true;
		g.Push();

		try
		{
			var artifactTooltips = artifact.GetTooltips();

			var tooltips = new Tooltips();
			tooltips.Add(new Vec(21), artifactTooltips);
			tooltips.Render(g);

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

			if (CurrentRenderTarget is null || CurrentRenderTarget.Width != (tooltipWidth + 20) * g.mg.PIX_SCALE || CurrentRenderTarget.Height != tooltipHeight * g.mg.PIX_SCALE)
			{
				CurrentRenderTarget?.Dispose();
				CurrentRenderTarget = new(g.mg.GraphicsDevice, (tooltipWidth + 20) * g.mg.PIX_SCALE, tooltipHeight * g.mg.PIX_SCALE);
			}

			var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

			g.mg.GraphicsDevice.SetRenderTarget(CurrentRenderTarget);

			g.mg.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
			Draw.StartAutoBatchFrame();
			try
			{
				if (artifact.GetMeta().pools.Contains(ArtifactPool.Boss))
					Draw.Sprite(GlowSprite.Sprite, 12, 11, originRel: new Vec(0.5, 0.5), scale: new Vec(48, 48) / 512.0, color: new Color(0.0, 0.5, 1.0).gain(0.3));

				artifact.Render(g, new Vec(6, 6), showCount: false);
				Tooltip.RenderMultiple(g, tooltips.pos, Tooltips._tooltipScratch);
			}
			catch
			{
				ModEntry.Instance.Logger.LogError("There was an error exporting artifact {Artifact}.", artifact.Key());
			}
			if (withScreenFilter)
				Draw.Rect(0, 0, (tooltipWidth + 20) * g.mg.PIX_SCALE, tooltipHeight * g.mg.PIX_SCALE, Colors.screenOverlay, new BlendState
				{
					ColorBlendFunction = BlendFunction.Add,
					ColorSourceBlend = Blend.One,
					ColorDestinationBlend = Blend.InverseSourceColor,
					AlphaSourceBlend = Blend.Zero,
					AlphaDestinationBlend = Blend.One
				});
			Draw.EndAutoBatchFrame();

			g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);

			CurrentRenderTarget.SaveAsPng(stream, (tooltipWidth + 20) * g.mg.PIX_SCALE, tooltipHeight * g.mg.PIX_SCALE);
		}
		finally
		{
			g.Pop();
			g.mg.PIX_SCALE = oldPixScale;
			g.mg.cameraMatrix = oldCameraMatrix;
			g.metaRoute = oldMetaRoute;
			Tooltips.SCREEN_LIMITS = oldScreenLimits;
			CardPatches.ActivateAllActions = oldActivateAllActions;
		}
	}
}