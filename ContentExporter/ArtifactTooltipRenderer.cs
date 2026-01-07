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
	public int? ArtifactsScale;
}

internal sealed class ArtifactTooltipRenderer
{
	public void Render(G g, int scale, bool withScreenFilter, ExportBackground background, Artifact artifact, Stream stream)
	{
		var oldMetaRoute = g.metaRoute;
		var oldScreenLimits = Tooltips.SCREEN_LIMITS;
		var oldActivateAllActions = CardPatches.ActivateAllActions;
		
		try
		{
			g.metaRoute = new MainMenu { subRoute = new Codex { subRoute = new ArtifactBrowse() } };
			Tooltips.SCREEN_LIMITS = new(0, 0, double.PositiveInfinity, double.PositiveInfinity);
			CardPatches.ActivateAllActions = true;
			
			var artifactTooltips = artifact.GetTooltips();

			var tooltips = new Tooltips();
			tooltips.Add(new Vec(21), artifactTooltips);
			tooltips.Render(g);

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

			using var texture = TextureUtils.CreateTexture(new(tooltipWidth + 20, tooltipHeight)
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

					if (artifact.GetMeta().pools.Contains(ArtifactPool.Boss))
						Draw.Sprite(ModEntry.Instance.BossArtifactGlowSprite.Sprite, 6 + 11 / 2.0, 6 + 11 / 2.0, originRel: new Vec(0.5, 0.5), scale: new Vec(23, 23) / 512.0, color: new Color(0.0, 0.5, 1.0).gain(0.3));

					artifact.Render(g, new Vec(6, 6), showCount: false);
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
			ModEntry.Instance.Logger.LogError("There was an error exporting artifact {Artifact} for deck {Deck}: {Exception}", artifact, artifact.GetMeta().owner.Key(), ex);
		}
		finally
		{
			g.metaRoute = oldMetaRoute;
			Tooltips.SCREEN_LIMITS = oldScreenLimits;
			CardPatches.ActivateAllActions = oldActivateAllActions;
		}
	}
}