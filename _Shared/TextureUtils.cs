using System;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.Shared;

internal static class TextureUtils
{
	public readonly struct CreateTextureArgs(int width, int height)
	{
		public readonly int Width = width;
		public readonly int Height = height;
		public required Action<Vec> Actions { get; init; }
		public int? Scale { get; init; }
		public bool SkipTexture { get; init; }

		public CreateTextureArgs(Vec size) : this((int)size.x, (int)size.y) { }
	}
	
	private static readonly Lazy<Func<SpriteBatch, bool>> SpriteBatchBeginCalledGetter = new(() =>
	{
		var field = AccessTools.DeclaredField(typeof(SpriteBatch), "_beginCalled");
		var dynamicMethod = new DynamicMethod("get__beginCalled", typeof(bool), [typeof(SpriteBatch)]);
		var il = dynamicMethod.GetILGenerator();
		
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, field);
		il.Emit(OpCodes.Ret);

		return dynamicMethod.CreateDelegate<Func<SpriteBatch, bool>>();
	});

	extension(SpriteBatch batch)
	{
		public bool BeginCalled
			=> SpriteBatchBeginCalledGetter.Value(batch);
	}
	
	public static Texture2D CreateTexture(CreateTextureArgs args)
	{
		var oldRenderTargets = MG.inst.GraphicsDevice.GetRenderTargets();
		if (oldRenderTargets.Length == 0 && MG.inst.renderTarget is null)
			throw new InvalidOperationException("Cannot create texture - no render target");
		var oldRenderTarget = (oldRenderTargets.Length == 0 ? MG.inst.renderTarget : oldRenderTargets[0].RenderTarget as RenderTarget2D) ?? MG.inst.renderTarget;

		var beganCalled = MG.inst.sb.BeginCalled;
		if (beganCalled)
			Draw.EndAutoBatchFrame();

		var oldPixScale = MG.inst.PIX_SCALE;
		var oldShake = MG.inst.g.state.shake;
		var oldCamera = MG.inst.cameraMatrix;
		var oldBatch = MG.inst.sb;

		RenderTarget2D? backupTarget = null;
		RenderTarget2D? resultTarget = null;

		try
		{
			if (oldRenderTarget.RenderTargetUsage != RenderTargetUsage.PreserveContents)
			{
				backupTarget = new RenderTarget2D(MG.inst.GraphicsDevice, oldRenderTarget.Width, oldRenderTarget.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
				MG.inst.GraphicsDevice.SetRenderTargets(backupTarget);
				MG.inst.GraphicsDevice.Clear(MGColor.Black);
			
				MG.inst.sb = new SpriteBatch(MG.inst.GraphicsDevice);
				MG.inst.sb.Begin();
				MG.inst.sb.Draw(oldRenderTarget, Vector2.Zero, MGColor.White);
				MG.inst.sb.End();
			}

			var realScale = args.Scale ?? 1;
			resultTarget = new RenderTarget2D(MG.inst.GraphicsDevice, args.Width * realScale, args.Height * realScale, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
			MG.inst.GraphicsDevice.SetRenderTargets(resultTarget);
			MG.inst.GraphicsDevice.Clear(MGColor.Transparent);

			using var resultBatch = new SpriteBatch(MG.inst.GraphicsDevice);
			MG.inst.PIX_SCALE = realScale;
			MG.inst.g.state.shake = 0;
			MG.inst.cameraMatrix = MG.inst.g.GetMatrix() * Matrix.CreateScale(realScale, realScale, 1f);
			MG.inst.sb = resultBatch;

			Draw.StartAutoBatchFrame();
			args.Actions(new(args.Width, args.Height));
			Draw.EndAutoBatchFrame();

			if (args.SkipTexture)
				return resultTarget;

			var data = new MGColor[resultTarget.Width * resultTarget.Height];
			var texture = new Texture2D(MG.inst.GraphicsDevice, resultTarget.Width, resultTarget.Height);
			resultTarget.GetData(data);
			texture.SetData(data);
			
			resultTarget.Dispose();
			return texture;
		}
		catch
		{
			if (resultTarget is not null && !resultTarget.IsDisposed)
				resultTarget.Dispose();
			throw;
		}
		finally
		{
			MG.inst.GraphicsDevice.SetRenderTargets(oldRenderTarget);
			MG.inst.PIX_SCALE = oldPixScale;
			MG.inst.g.state.shake = oldShake;
			MG.inst.cameraMatrix = oldCamera;
			MG.inst.sb = oldBatch;

			if (backupTarget is not null && !backupTarget.IsDisposed)
			{
				oldBatch.Begin();
				oldBatch.Draw(backupTarget, Vector2.Zero, MGColor.White);
				oldBatch.End();
				backupTarget.Dispose();
			}
			
			if (beganCalled)
				Draw.StartAutoBatchFrame();
		}
	}

	public static Texture2D CropToContent(Texture2D texture, MGColor? backgroundColor = null, bool cropLeft = true, bool cropRight = true, bool cropTop = true, bool cropBottom = true)
	{
		var colors = new MGColor[texture.Width * texture.Height];
		texture.GetData(colors);

		var top = 0;
		while (cropTop && top < texture.Height - 1)
		{
			if (Enumerable.Range(0, texture.Width).Any(x => colors[x + top * texture.Width].A != 0))
				break;
			top++;
		}
			
		var bottom = texture.Height - 1;
		while (cropBottom && bottom > 0)
		{
			if (Enumerable.Range(0, texture.Width).Any(x => colors[x + bottom * texture.Width].A != 0))
				break;
			bottom--;
		}

		var left = 0;
		while (cropLeft && left < texture.Width - 1)
		{
			if (Enumerable.Range(0, texture.Height).Any(y => colors[left + y * texture.Width].A != 0))
				break;
			left++;
		}

		var right = texture.Width - 1;
		while (cropRight && right > 0)
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
