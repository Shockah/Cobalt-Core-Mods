using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.Shared;

internal static class TextureUtils
{
	public static Texture2D CreateTexture(int width, int height, Action actions)
	{
		var oldRenderTargets = MG.inst.GraphicsDevice.GetRenderTargets();
		if (oldRenderTargets.Length == 0 && MG.inst.renderTarget is null)
			throw new InvalidOperationException("Cannot create texture - no render target");
		var oldRenderTarget = (oldRenderTargets.Length == 0 ? MG.inst.renderTarget : oldRenderTargets[0].RenderTarget as RenderTarget2D) ?? MG.inst.renderTarget;

		Draw.EndAutoBatchFrame();

		var oldShake = MG.inst.g.state.shake;
		var oldCamera = MG.inst.cameraMatrix;
		var oldBatch = MG.inst.sb;

		using var backupTarget = new RenderTarget2D(MG.inst.GraphicsDevice, oldRenderTarget.Width, oldRenderTarget.Height);
		MG.inst.GraphicsDevice.SetRenderTargets(backupTarget);
		MG.inst.GraphicsDevice.Clear(MGColor.Black);

		using var backupBatch = new SpriteBatch(MG.inst.GraphicsDevice);
		backupBatch.Begin();
		backupBatch.Draw(oldRenderTarget, Microsoft.Xna.Framework.Vector2.Zero, MGColor.White);
		backupBatch.End();

		try
		{
			using var resultTarget = new RenderTarget2D(MG.inst.GraphicsDevice, width, height);
			MG.inst.GraphicsDevice.SetRenderTargets(resultTarget);
			MG.inst.GraphicsDevice.Clear(MGColor.Transparent);

			using var resultBatch = new SpriteBatch(MG.inst.GraphicsDevice);
			MG.inst.g.state.shake = 0;
			MG.inst.cameraMatrix = MG.inst.g.GetMatrix();
			MG.inst.sb = resultBatch;

			Draw.StartAutoBatchFrame();
			actions();
			Draw.EndAutoBatchFrame();

			var data = new MGColor[width * height];
			var texture = new Texture2D(MG.inst.GraphicsDevice, width, height);
			resultTarget.GetData(data);
			texture.SetData(data);
			return texture;
		}
		finally
		{
			MG.inst.GraphicsDevice.SetRenderTargets(oldRenderTarget);
			MG.inst.g.state.shake = oldShake;
			MG.inst.cameraMatrix = oldCamera;
			MG.inst.sb = oldBatch;
			oldBatch.Begin();
			oldBatch.Draw(backupTarget, Microsoft.Xna.Framework.Vector2.Zero, MGColor.White);
			oldBatch.End();
			Draw.StartAutoBatchFrame();
		}
	}

	public static Texture2D CropToContent(Texture2D texture, MGColor? backgroundColor = null)
	{
		var colors = new MGColor[texture.Width * texture.Height];
		texture.GetData(colors);

		var top = 0;
		while (top < texture.Height - 1)
		{
			if (Enumerable.Range(0, texture.Width).Any(x => colors[x + top * texture.Width].A != 0))
				break;
			top++;
		}
			
		var bottom = texture.Height - 1;
		while (bottom > 0)
		{
			if (Enumerable.Range(0, texture.Width).Any(x => colors[x + bottom * texture.Width].A != 0))
				break;
			bottom--;
		}

		var left = 0;
		while (left < texture.Width - 1)
		{
			if (Enumerable.Range(0, texture.Height).Any(y => colors[left + y * texture.Width].A != 0))
				break;
			left++;
		}

		var right = texture.Width - 1;
		while (right > 0)
		{
			if (Enumerable.Range(0, texture.Height).Any(y => colors[right + y * texture.Width].A != 0))
				break;
			right--;
		}

		var textureWidth = right - left + 1;
		var textureHeight = bottom - top + 1;

		if (textureWidth == texture.Width && textureHeight == texture.Height)
			return texture;
		
		var textureData = new MGColor[textureWidth * textureHeight];

		for (var y = 0; y < textureHeight; y++)
		{
			for (var x = 0; x < textureWidth; x++)
			{
				var color = colors[(x + left) + (y + top) * texture.Width];
				if (color.A != 255 && backgroundColor is not null)
					color = MGColor.Lerp(backgroundColor.Value, new(color.R, color.G, color.B), color.A / 255f);
				textureData[x + y * textureWidth] = color;
			}
		}
			
		var resultTexture = new Texture2D(texture.GraphicsDevice, textureWidth, textureHeight);
		resultTexture.SetData(textureData);
		return resultTexture;
	}
}
