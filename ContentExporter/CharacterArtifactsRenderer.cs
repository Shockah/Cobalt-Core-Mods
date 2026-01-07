using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Shockah.Shared;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.ContentExporter;

internal sealed partial class Settings
{
	[JsonProperty]
	public int? CharacterArtifactsScale = DEFAULT_SCALE;
	
	[JsonProperty]
	public int CharacterArtifactsVerticalSpacing = 4;
	
	[JsonProperty]
	public int CharacterArtifactsHorizontalSpacing = 4;
	
	[JsonProperty]
	public CharacterArtifactsStyle CharacterArtifactsStyle = CharacterArtifactsStyle.Split2Columns;
}

internal enum CharacterArtifactsStyle
{
	Vertical,
	HorizontalLeft,
	HorizontalTop,
	Split2Rows,
	Merged2Rows,
	Split2Columns,
	Merged2Columns,
}

internal sealed class CharacterArtifactsRenderer
{
	private RenderTarget2D? RenderTarget;

	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, List<Artifact> artifacts, Stream stream)
	{
		var oldPixScale = g.mg.PIX_SCALE;
		var oldCameraMatrix = g.mg.cameraMatrix;
		var oldMetaRoute = g.metaRoute;
		var oldScreenLimits = Tooltips.SCREEN_LIMITS;
		var oldActivateAllActions = CardPatches.ActivateAllActions;

		g.mg.PIX_SCALE = scale;
		g.mg.cameraMatrix = g.GetMatrix() * Matrix.CreateScale(g.mg.PIX_SCALE, g.mg.PIX_SCALE, 1f);
		g.metaRoute = new MainMenu { subRoute = new Codex { subRoute = new ArtifactBrowse() } };
		Tooltips.SCREEN_LIMITS = new(0, 0, double.PositiveInfinity, double.PositiveInfinity);
		CardPatches.ActivateAllActions = true;
		g.Push();

		try
		{
			
			Draw.StartAutoBatchFrame();
			var anyArtifact = artifacts.First();
			var artifactRects = artifacts.Select(a => RenderOnlyTooltip(g, a, false)).ToList();
			Draw.EndAutoBatchFrame();

			var renderTargetWidth = g.mg.PIX_W;
			var renderTargetHeight = g.mg.PIX_H;
			var cropLeft = true;
			var cropTop = true;
			
			switch (ModEntry.Instance.Settings.CharacterArtifactsStyle)
			{
				case CharacterArtifactsStyle.Vertical:
					renderTargetHeight = artifactRects.Sum(r => (int)r.h) + artifacts.Count * ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing;
					cropLeft = false;
					break;
				case CharacterArtifactsStyle.HorizontalLeft:
					renderTargetWidth = artifactRects.Sum(r => (int)r.w) + artifacts.Count * (ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing + 21);
					cropLeft = false;
					break;
				case CharacterArtifactsStyle.HorizontalTop:
					renderTargetWidth = artifactRects.Sum(r => (int)r.w) + artifacts.Count * ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing;
					cropTop = false;
					break;
				case CharacterArtifactsStyle.Split2Rows:
					renderTargetWidth = (artifactRects.Select(r => (int)r.w).Max() + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing) * (artifactRects.Count + 1) / 2;
					renderTargetHeight = artifactRects.Select(r => (int)r.h).Max() * 2 + 21 * 3;
					break;
				case CharacterArtifactsStyle.Merged2Rows:
					renderTargetWidth = (artifactRects.Select(r => (int)r.w).Max() + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing) * (artifactRects.Count + 1) / 2;
					renderTargetHeight = artifactRects.Select(r => (int)r.h).Max() * 2 + 21 * 2;
					break;
				case CharacterArtifactsStyle.Split2Columns:
					renderTargetWidth = artifactRects.Select(r => (int)r.w).Max() * 2 + 21 * 3;
					renderTargetHeight = (artifactRects.Select(r => (int)r.h).Max() + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing) * (artifactRects.Count + 1) / 2;
					break;
				case CharacterArtifactsStyle.Merged2Columns:
					renderTargetWidth = artifactRects.Select(r => (int)r.w).Max() * 2 + 21 * 2;
					renderTargetHeight = (artifactRects.Select(r => (int)r.h).Max() + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing) * (artifactRects.Count + 1) / 2;
					break;
			}
			
			if (RenderTarget is null || RenderTarget.Width != renderTargetWidth * g.mg.PIX_SCALE || RenderTarget.Height != renderTargetHeight * g.mg.PIX_SCALE)
			{
				RenderTarget?.Dispose();
				RenderTarget = new(g.mg.GraphicsDevice, renderTargetWidth * g.mg.PIX_SCALE, renderTargetHeight * g.mg.PIX_SCALE);
			}

			var oldRenderTargets = g.mg.GraphicsDevice.GetRenderTargets();

			g.mg.GraphicsDevice.SetRenderTarget(RenderTarget);

			g.mg.GraphicsDevice.Clear(MGColor.Transparent);
			Draw.StartAutoBatchFrame();
			try
			{
				switch (background)
				{
					case ExportBackground.Black:
						Draw.Rect(0, 0, RenderTarget.Width / g.mg.PIX_SCALE, RenderTarget.Height / g.mg.PIX_SCALE, Colors.black);
						break;
					case ExportBackground.White:
						Draw.Rect(0, 0, RenderTarget.Width / g.mg.PIX_SCALE, RenderTarget.Height / g.mg.PIX_SCALE, Colors.white);
						break;
				}
				
				switch (ModEntry.Instance.Settings.CharacterArtifactsStyle)
				{
					case CharacterArtifactsStyle.Vertical:
						RenderVerticalStyle(g, artifacts);
						break;
					case CharacterArtifactsStyle.HorizontalLeft:
						RenderHorizontalLeftStyle(g, artifacts);
						break;
					case CharacterArtifactsStyle.HorizontalTop:
						RenderHorizontalTopStyle(g, artifacts);
						break;
					case CharacterArtifactsStyle.Split2Rows:
						RenderSplit2RowsStyle(g, artifacts, artifactRects);
						break;
					case CharacterArtifactsStyle.Merged2Rows:
						RenderMerged2RowsStyle(g, artifacts, artifactRects);
						break;
					case CharacterArtifactsStyle.Split2Columns:
						RenderSplit2ColumnsStyle(g, artifacts, artifactRects);
						break;
					case CharacterArtifactsStyle.Merged2Columns:
						RenderMerged2ColumnsStyle(g, artifacts, artifactRects);
						break;
				}
			}
			catch
			{
				ModEntry.Instance.Logger.LogError("There was an error exporting artifacts for deck {Deck}.", anyArtifact.GetMeta().owner);
			}
			if (withScreenFilter)
				Draw.Rect(0, 0, RenderTarget.Width / g.mg.PIX_SCALE, RenderTarget.Height / g.mg.PIX_SCALE, Colors.screenOverlay, new BlendState
				{
					ColorBlendFunction = BlendFunction.Add,
					ColorSourceBlend = Blend.One,
					ColorDestinationBlend = Blend.InverseSourceColor,
					AlphaSourceBlend = Blend.Zero,
					AlphaDestinationBlend = Blend.One
				});
			Draw.EndAutoBatchFrame();

			g.mg.GraphicsDevice.SetRenderTargets(oldRenderTargets);
			
			var croppedTexture = TextureUtils.CropToContent(RenderTarget, background switch
			{
				ExportBackground.Black => withScreenFilter ? new MGColor(0xFF260306) : MGColor.Black,
				ExportBackground.White => MGColor.White,
				_ => null
			}, cropLeft: cropLeft, cropTop: cropTop);
			croppedTexture.SaveAsPng(stream, croppedTexture.Width, croppedTexture.Height);
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

	private static void RenderVerticalStyle(G g, List<Artifact> artifacts)
	{
		var yOffset = 0;

		foreach (var artifact in artifacts)
		{
			RenderOnlyArtifact(g, artifact, new Vec(6, 6 + yOffset));
			var tooltipRect = RenderOnlyTooltip(g, artifact, position: new Vec(21, yOffset));
			yOffset += (int)tooltipRect.h + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing;
		}
	}

	private static void RenderHorizontalLeftStyle(G g, List<Artifact> artifacts)
	{
		var xOffset = 0;

		foreach (var artifact in artifacts)
		{
			RenderOnlyArtifact(g, artifact, new Vec(6 + xOffset, 6));
			var tooltipRect = RenderOnlyTooltip(g, artifact, position: new Vec(21 + xOffset, 0));
			xOffset += (int)tooltipRect.w + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing + 21;
		}
	}

	private static void RenderHorizontalTopStyle(G g, List<Artifact> artifacts)
	{
		var xOffset = 0;

		foreach (var artifact in artifacts)
		{
			RenderOnlyArtifact(g, artifact, new Vec(6 + xOffset, 6));
			var tooltipRect = RenderOnlyTooltip(g, artifact, position: new Vec(xOffset, 21));
			xOffset += (int)tooltipRect.w + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing;
		}
	}
	
	private static void RenderSplit2RowsStyle(G g, List<Artifact> artifacts, List<Rect> artifactRects)
	{
		var topOffset = 0;
		var bottomOffset = 0;
		var totalHeight = artifactRects.Select(r => (int)r.h).Max() * 2 + 21 * 2 - 4;
		
		for (var i = 0; i < artifacts.Count; i++)
		{
			var artifact = artifacts[i];
			var artifactRect = artifactRects[i];
			var useTop = topOffset <= bottomOffset;
			var xOffset = useTop ? topOffset : bottomOffset;
			
			if (useTop)
			{
				RenderOnlyArtifact(g, artifact, new Vec(6 + xOffset, (totalHeight - 21) / 2 + 4));
				RenderOnlyTooltip(g, artifact, position: new Vec(xOffset, (totalHeight - 21) / 2 - artifactRect.h));
				topOffset = xOffset + (int)artifactRect.w + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing;
			}
			else
			{
				RenderOnlyArtifact(g, artifact, new Vec(6 + xOffset, (totalHeight - 21) / 2 + 21));
				RenderOnlyTooltip(g, artifact, position: new Vec(xOffset, (totalHeight - 21) / 2 + 21 * 2 - 5));
				bottomOffset = xOffset + (int)artifactRect.w + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing;
			}
		}
	}

	private static void RenderMerged2RowsStyle(G g, List<Artifact> artifacts, List<Rect> artifactRects)
	{
		var iconOffset = 0;
		var topOffset = 0;
		var bottomOffset = 0;
		var totalHeight = artifactRects.Select(r => (int)r.h).Max() * 2 + 21;
		
		for (var i = 0; i < artifacts.Count; i++)
		{
			var artifact = artifacts[i];
			var artifactRect = artifactRects[i];
			var useTop = topOffset <= bottomOffset;
			var xOffset = Math.Max(useTop ? topOffset : bottomOffset, iconOffset);
			
			RenderOnlyArtifact(g, artifact, new Vec(6 + xOffset, (totalHeight - 21) / 2 + 4));
			if (useTop)
			{
				RenderOnlyTooltip(g, artifact, position: new Vec(xOffset, (totalHeight - 21) / 2 - artifactRect.h));
				topOffset = xOffset + (int)artifactRect.w + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing;
			}
			else
			{
				RenderOnlyTooltip(g, artifact, position: new Vec(xOffset, (totalHeight - 21) / 2 + 20));
				bottomOffset = xOffset + (int)artifactRect.w + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing;
			}
			
			iconOffset = xOffset + 13 + ModEntry.Instance.Settings.CharacterArtifactsHorizontalSpacing;
		}
	}
	
	private static void RenderSplit2ColumnsStyle(G g, List<Artifact> artifacts, List<Rect> artifactRects)
	{
		var leftOffset = 0;
		var rightOffset = 0;
		var totalWidth = artifactRects.Select(r => (int)r.w).Max() * 2 + 21 * 2 - 4;
		
		for (var i = 0; i < artifacts.Count; i++)
		{
			var artifact = artifacts[i];
			var artifactRect = artifactRects[i];
			var useLeft = leftOffset <= rightOffset;
			var yOffset = useLeft ? leftOffset : rightOffset;
			
			if (useLeft)
			{
				RenderOnlyArtifact(g, artifact, new Vec((totalWidth - 21) / 2 + 4, 6 + yOffset));
				RenderOnlyTooltip(g, artifact, position: new Vec((totalWidth - 21) / 2 - artifactRect.w, yOffset));
				leftOffset = yOffset + (int)artifactRect.h + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing;
			}
			else
			{
				RenderOnlyArtifact(g, artifact, new Vec((totalWidth - 21) / 2 + 21, 6 + yOffset));
				RenderOnlyTooltip(g, artifact, position: new Vec((totalWidth - 21) / 2 + 21 * 2 - 5, yOffset));
				rightOffset = yOffset + (int)artifactRect.h + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing;
			}
		}
	}

	private static void RenderMerged2ColumnsStyle(G g, List<Artifact> artifacts, List<Rect> artifactRects)
	{
		var iconOffset = 0;
		var leftOffset = 0;
		var rightOffset = 0;
		var totalWidth = artifactRects.Select(r => (int)r.w).Max() * 2 + 21;
		
		for (var i = 0; i < artifacts.Count; i++)
		{
			var artifact = artifacts[i];
			var artifactRect = artifactRects[i];
			var useLeft = leftOffset <= rightOffset;
			var yOffset = Math.Max(useLeft ? leftOffset : rightOffset, iconOffset);
			
			RenderOnlyArtifact(g, artifact, new Vec((totalWidth - 21) / 2 + 4, 6 + yOffset));
			if (useLeft)
			{
				RenderOnlyTooltip(g, artifact, position: new Vec((totalWidth - 21) / 2 - artifactRect.w, yOffset));
				leftOffset = yOffset + (int)artifactRect.h + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing;
			}
			else
			{
				RenderOnlyTooltip(g, artifact, position: new Vec((totalWidth - 21) / 2 + 20, yOffset));
				rightOffset = yOffset + (int)artifactRect.h + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing;
			}
			
			iconOffset = yOffset + 13 + ModEntry.Instance.Settings.CharacterArtifactsVerticalSpacing;
		}
	}

	private static void RenderOnlyArtifact(G g, Artifact artifact, Vec position)
	{
		if (artifact.GetMeta().pools.Contains(ArtifactPool.Boss))
			Draw.Sprite(ModEntry.Instance.BossArtifactGlowSprite.Sprite, position.x + 11 / 2.0, position.y + 11 / 2.0, originRel: new Vec(0.5, 0.5), scale: new Vec(23, 23) / 512.0, color: new Color(0.0, 0.5, 1.0).gain(0.3));
		artifact.Render(g, new Vec(position.x, position.y), showCount: false);
	}

	private static Rect RenderOnlyTooltip(G g, Artifact artifact, bool dontDraw = false, Vec position = default)
	{
		var artifactTooltips = artifact.GetTooltips();
		
		var tooltips = new Tooltips();
		tooltips.Add(position, artifactTooltips);
		tooltips.Render(g); // sets up a cache

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
		
		if (!dontDraw)
			Tooltip.RenderMultiple(g, tooltips.pos, Tooltips._tooltipScratch);
		return new(position.x, position.y, tooltipWidth, tooltipHeight);
	}
}