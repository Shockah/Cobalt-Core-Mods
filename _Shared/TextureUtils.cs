using Microsoft.Xna.Framework.Graphics;
using System;

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
		MG.inst.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

		using var backupBatch = new SpriteBatch(MG.inst.GraphicsDevice);
		backupBatch.Begin();
		backupBatch.Draw(oldRenderTarget, Microsoft.Xna.Framework.Vector2.Zero, Microsoft.Xna.Framework.Color.White);
		backupBatch.End();

		try
		{
			using var resultTarget = new RenderTarget2D(MG.inst.GraphicsDevice, width, height);
			MG.inst.GraphicsDevice.SetRenderTargets(resultTarget);
			MG.inst.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);

			using var resultBatch = new SpriteBatch(MG.inst.GraphicsDevice);
			MG.inst.g.state.shake = 0;
			MG.inst.cameraMatrix = MG.inst.g.GetMatrix();
			MG.inst.sb = resultBatch;

			Draw.StartAutoBatchFrame();
			actions();
			Draw.EndAutoBatchFrame();

			var data = new Microsoft.Xna.Framework.Color[width * height];
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
			oldBatch.Draw(backupTarget, Microsoft.Xna.Framework.Vector2.Zero, Microsoft.Xna.Framework.Color.White);
			oldBatch.End();
			Draw.StartAutoBatchFrame();
		}
	}
}
