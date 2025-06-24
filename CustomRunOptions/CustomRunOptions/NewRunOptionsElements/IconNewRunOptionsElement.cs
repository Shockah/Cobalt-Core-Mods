namespace Shockah.CustomRunOptions;

internal class IconNewRunOptionsElement(Spr icon, int? width = null, int? height = null) : ICustomRunOption.INewRunOptionsElement
{
	public Vec Size
	{
		get
		{
			if (width is { } w && height is { } h)
				return new(w, h);

			var texture = SpriteLoader.Get(icon)!;
			return new(texture.Width, texture.Height);
		}
	}

	public void Render(G g, Vec position)
		=> Draw.Sprite(icon, position.x, position.y);
}