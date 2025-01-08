using Microsoft.Xna.Framework.Graphics;
using Nickel;
using MGColor = Microsoft.Xna.Framework.Color;

namespace Shockah.Destiny;

internal static class TextureOutlines
{
	// private static MGColor[]? AtlasTextureData;
	
	private static readonly (int X, int Y, int PerpendicularX1, int PerpendicularY1, int PerpendicularX2, int PerpendicularY2)[] VectorNeighbors = [
		(1, 0, 0, -1, 0, 1),
		(-1, 0, 0, -1, 0, 1),
		(0, 1, -1, 0, 1, 0),
		(0, -1, -1, 0, 1, 0),
	];
	private static readonly (int X, int Y)[] VectorCorners = [(1, 1), (1, -1), (-1, -1), (-1, 1)];

	private static ISpriteEntry CreateOutlineSprite(MGColor[] baseTextureData, int baseTextureWidth, int baseTextureHeight, bool neighbors, bool neighborInsets, bool diagonalCorners, string? name = null)
	{
		var outlineTexture = new Texture2D(MG.inst.GraphicsDevice, baseTextureWidth + 2, baseTextureHeight + 2);
		var outlineTextureData = new MGColor[outlineTexture.Width * outlineTexture.Height];

		for (var y = 0; y < outlineTexture.Height; y++)
			for (var x = 0; x < outlineTexture.Width; x++)
				outlineTextureData[x + y * outlineTexture.Width] = ShouldContainOutline(x, y) ? MGColor.White : MGColor.Transparent;
		
		outlineTexture.SetData(outlineTextureData);
		if (name is null)
			return ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(() => outlineTexture);
		return ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(name, () => outlineTexture);
		
		bool ShouldContainOutline(int outlineX, int outlineY)
		{
			var baseX = outlineX - 1;
			var baseY = outlineY - 1;
			
			if (IsNonZeroAlphaPixel(baseX, baseY))
				return false;
			
			if (neighbors)
				foreach (var neighbor in VectorNeighbors)
					if (IsNonZeroAlphaPixel(baseX + neighbor.X, baseY + neighbor.Y))
						return true;

			if (neighborInsets)
			{
				foreach (var neighbor in VectorNeighbors)
				{
					if (IsNonZeroAlphaPixel(baseX + neighbor.X, baseY + neighbor.Y))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + neighbor.X + neighbor.PerpendicularX1, baseY + neighbor.Y + neighbor.PerpendicularY1))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + neighbor.X + neighbor.PerpendicularX2, baseY + neighbor.Y + neighbor.PerpendicularY2))
						continue;
					return true;
				}
			}

			if (diagonalCorners)
			{
				foreach (var corner in VectorCorners)
				{
					if (!IsNonZeroAlphaPixel(baseX + corner.X, baseY + corner.Y))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + corner.X * 2, baseY + corner.Y))
						continue;
					if (!IsNonZeroAlphaPixel(baseX + corner.X, baseY + corner.Y * 2))
						continue;
					if (IsNonZeroAlphaPixel(baseX + corner.X * 2, baseY))
						continue;
					if (IsNonZeroAlphaPixel(baseX, baseY + corner.Y * 2))
						continue;
					return true;
				}
			}

			return false;
			
			MGColor? GetBaseTextureColor(int baseX, int baseY)
			{
				if (baseX < 0 || baseY < 0 || baseX >= baseTextureWidth || baseY >= baseTextureHeight)
					return null;
				return baseTextureData[baseX + baseY * baseTextureWidth];
			}

			bool IsNonZeroAlphaColor(MGColor? color)
				=> color is { A: > 0 };

			bool IsNonZeroAlphaPixel(int baseX, int baseY)
				=> IsNonZeroAlphaColor(GetBaseTextureColor(baseX, baseY));
		}
	}

	public static ISpriteEntry CreateOutlineSprite(Texture2D baseTexture, bool neighbors, bool neighborInsets, bool diagonalCorners, string? name = null)
	{
		var baseTextureData = new MGColor[baseTexture.Width * baseTexture.Height];
		baseTexture.GetData(baseTextureData);
		return CreateOutlineSprite(baseTextureData, baseTexture.Width, baseTexture.Height, neighbors, neighborInsets, diagonalCorners, name);
	}
	
	public static ISpriteEntry CreateOutlineSprite(Spr spr, bool neighbors, bool neighborInsets, bool diagonalCorners, string? name = null)
	{
		// ReSharper disable JoinDeclarationAndInitializer
		int baseTextureWidth, baseTextureHeight;
		MGColor[] baseTextureData;
		// ReSharper restore JoinDeclarationAndInitializer

		// if (DB.atlas is not null && DB.atlas.TryGetValue(spr, out var atlasItem) && SpriteLoader.Get(StableSpr.atlas, okIfMissing: true) is { } atlasTexture)
		// {
		// 	var atlasTextureData = AtlasTextureData;
		// 	if (atlasTextureData is null)
		// 	{
		// 		atlasTextureData = new MGColor[atlasTexture.Width * atlasTexture.Height];
		// 		atlasTexture.GetData(atlasTextureData);
		// 		AtlasTextureData = atlasTextureData;
		// 	}
		//
		// 	baseTextureWidth = (int)atlasItem.bounds.w;
		// 	baseTextureHeight = (int)atlasItem.bounds.h;
		// 	baseTextureData = new MGColor[baseTextureWidth * baseTextureHeight];
		// 	for (var y = 0; y < baseTextureHeight; y++)
		// 		for (var x = 0; x < baseTextureWidth; x++)
		// 			baseTextureData[x + y * baseTextureWidth] = atlasTextureData[(x + (int)atlasItem.bounds.x) + (y + (int)atlasItem.bounds.y) * atlasTexture.Width];
		// }
		// else
		
		var baseTexture = SpriteLoader.Get(spr)!;
		baseTextureWidth = baseTexture.Width;
		baseTextureHeight = baseTexture.Height;
		baseTextureData = new MGColor[baseTexture.Width * baseTexture.Height];
		baseTexture.GetData(baseTextureData);

		return CreateOutlineSprite(baseTextureData, baseTextureWidth, baseTextureHeight, neighbors, neighborInsets, diagonalCorners, name);
	}
}