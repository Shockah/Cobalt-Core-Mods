namespace Shockah.CustomRunOptions;

public class IconNewRunOptionsElement(Spr icon, int? width = null, int? height = null) : ICustomRunOptionsApi.INewRunOptionsElement.IIcon
{
	public Spr Icon { get; set; } = icon;
	public int? Width { get; set; } = width;
	public int? Height { get; set; } = height;

	public Vec Size
	{
		get
		{
			if (Width is { } w && Height is { } h)
				return new(w, h);

			var texture = SpriteLoader.Get(Icon)!;
			return new(texture.Width, texture.Height);
		}
	}

	public void Render(G g, Vec position)
		=> Draw.Sprite(Icon, position.x, position.y);
	
	public ICustomRunOptionsApi.INewRunOptionsElement.IIcon SetIcon(Spr value)
	{
		this.Icon = value;
		return this;
	}

	public ICustomRunOptionsApi.INewRunOptionsElement.IIcon SetWidth(int? value)
	{
		this.Width = value;
		return this;
	}

	public ICustomRunOptionsApi.INewRunOptionsElement.IIcon SetHeight(int? value)
	{
		this.Height = value;
		return this;
	}
}