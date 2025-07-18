using System;
using System.Collections.Generic;
using System.Linq;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal sealed class IconAffixModSetting : IModSettingsApi.IModSetting, OnMouseDown, OnMouseDownRight, OnInputPhase
{
	public struct IconConfiguration()
	{
		public Spr Icon = StableSpr.icons_ace;
		public int? IconWidth = null;
		public int? IconHeight = null;
		public int BoundsSpacing = 10;
		public int ContentSpacing = -6;
		public double VerticalAlignment = 0.5;
	}
	
	public UIKey Key { get; private set; }
	private UIKey ContainerKey;
	public event IModSettingsApi.OnMenuOpen? OnMenuOpen;
	public event IModSettingsApi.OnMenuClose? OnMenuClose;

	public required IModSettingsApi.IModSetting Setting;
	public IconConfiguration? LeftIcon;
	public IconConfiguration? RightIcon;
	public Func<IEnumerable<Tooltip>>? Tooltips;

	public IconAffixModSetting()
	{
		this.OnMenuOpen += (g, route) =>
		{
			if (this.Key == 0)
				this.Key = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.ContainerKey == 0)
				this.ContainerKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			this.Setting?.RaiseOnMenuOpen(g, route);
		};
		this.OnMenuClose += g => this.Setting?.RaiseOnMenuClose(g);
	}

	~IconAffixModSetting()
	{
		if (this.Key != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.Key.k);
		if (this.ContainerKey != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.ContainerKey.k);
	}

	public void RaiseOnMenuOpen(G g, IModSettingsApi.IModSettingsRoute route)
		=> this.OnMenuOpen?.Invoke(g, route);

	public void RaiseOnMenuClose(G g)
		=> this.OnMenuClose?.Invoke(g);

	public Vec? Render(G g, Box box, bool dontDraw)
	{
		var unproxiedSetting = ModEntry.Instance.Helper.Utilities.Unproxy(Setting);
		
		if (box.key is not null)
		{
			box.autoFocus = true;
			box.onMouseDown = unproxiedSetting is OnMouseDown ? this : null;
			box.onMouseDownRight = unproxiedSetting is OnMouseDownRight ? this : null;
			box.onInputPhase = unproxiedSetting is OnInputPhase ? this : null;
		}
		
		var leftWidth = LeftIcon is { } leftIcon ? leftIcon.ContentSpacing + leftIcon.BoundsSpacing + (leftIcon.IconWidth ?? SpriteLoader.Get(leftIcon.Icon)!.Width) : 0;
		var rightWidth = RightIcon is { } rightIcon ? rightIcon.ContentSpacing + rightIcon.BoundsSpacing + (rightIcon.IconWidth ?? SpriteLoader.Get(rightIcon.Icon)!.Width) : 0;
		var contentWidth = box.rect.w - leftWidth - rightWidth;
		
		var sizingBox = g.Push(null, new Rect(box.rect.x, box.rect.y, contentWidth, 0));
		var nullableSettingSize = this.Setting.Render(g, sizingBox, dontDraw: true);
		g.Pop();

		if (nullableSettingSize is not { } settingSize)
			return null;

		if (!dontDraw)
		{
			var containerBox = g.Push(
				ContainerKey,
				new Rect(0, 0, box.rect.w, settingSize.y),
				onMouseDown: unproxiedSetting is OnMouseDown ? this : null,
				onMouseDownRight: unproxiedSetting is OnMouseDownRight ? this : null,
				onInputPhase: unproxiedSetting is OnInputPhase ? this : null
			);
			var isHover = (box.key is not null && box.IsHover()) || containerBox.IsHover() || g.hoverKey == Setting.Key || Input.currentGpKey == Setting.Key;
			if (isHover)
			{
				Draw.Rect(box.rect.x, box.rect.y, leftWidth, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);
				Draw.Rect(box.rect.x2 - rightWidth, box.rect.y, rightWidth, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);
				if (this.Tooltips is { } tooltips)
					g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
			}
			
			var contentBox = g.Push(
				Setting.Key,
				new Rect(leftWidth, 0, contentWidth, settingSize.y),
				onMouseDown: unproxiedSetting is OnMouseDown ? this : null,
				onMouseDownRight: unproxiedSetting is OnMouseDownRight ? this : null,
				onInputPhase: unproxiedSetting is OnInputPhase ? this : null
			);
			contentBox._isHover = isHover;
			Setting.Render(g, contentBox, dontDraw: false);
			containerBox._isHover_listen = contentBox._isHover_listen;
			contentBox.noInput = true;
			g.Pop();

			if (LeftIcon is { } leftIcon2)
			{
				var iconHeight = leftIcon2.IconHeight ?? SpriteLoader.Get(leftIcon2.Icon)!.Height;
				Draw.Sprite(leftIcon2.Icon, containerBox.rect.x + leftIcon2.BoundsSpacing, containerBox.rect.y + (settingSize.y - iconHeight) * leftIcon2.VerticalAlignment);
			}
			if (RightIcon is { } rightIcon2)
			{
				var iconHeight = rightIcon2.IconHeight ?? SpriteLoader.Get(rightIcon2.Icon)!.Height;
				Draw.Sprite(rightIcon2.Icon, containerBox.rect.x2 - rightWidth + rightIcon2.ContentSpacing, containerBox.rect.y + (settingSize.y - iconHeight) * rightIcon2.VerticalAlignment);
			}
			
			g.Pop();
		}
		return new(box.rect.w, settingSize.y);
	}

	public void OnMouseDown(G g, Box b)
	{
		if (b.key != ContainerKey && b.key != Key)
			return;

		var oldHoverKey = g.hoverKey;
		var oldCurrentKey = Input.currentGpKey;
		try
		{
			var box = g.boxes.FirstOrDefault(b => b.key == Setting.Key) ?? b;
			var unproxiedSetting = ModEntry.Instance.Helper.Utilities.Unproxy(Setting);
			g.hoverKey = box.key ?? oldHoverKey;
			Input.currentGpKey = box.key ?? oldCurrentKey;
			(unproxiedSetting as OnMouseDown)?.OnMouseDown(g, box);
		}
		finally
		{
			g.hoverKey = oldHoverKey;
			Input.currentGpKey = oldCurrentKey;
		}
	}

	public void OnMouseDownRight(G g, Box b)
	{
		if (b.key != ContainerKey && b.key != Key)
			return;

		var oldHoverKey = g.hoverKey;
		var oldCurrentKey = Input.currentGpKey;
		try
		{
			var box = g.boxes.FirstOrDefault(b => b.key == Setting.Key) ?? b;
			var unproxiedSetting = ModEntry.Instance.Helper.Utilities.Unproxy(Setting);
			g.hoverKey = box.key ?? oldHoverKey;
			Input.currentGpKey = box.key ?? oldCurrentKey;
			(unproxiedSetting as OnMouseDownRight)?.OnMouseDownRight(g, box);
		}
		finally
		{
			g.hoverKey = oldHoverKey;
			Input.currentGpKey = oldCurrentKey;
		}
	}

	public void OnInputPhase(G g, Box b)
	{
		if (b.key != ContainerKey && b.key != Key)
			return;

		var oldHoverKey = g.hoverKey;
		var oldCurrentKey = Input.currentGpKey;
		try
		{
			var box = g.boxes.FirstOrDefault(b => b.key == Setting.Key) ?? b;
			var unproxiedSetting = ModEntry.Instance.Helper.Utilities.Unproxy(Setting);
			g.hoverKey = box.key ?? oldHoverKey;
			Input.currentGpKey = box.key ?? oldCurrentKey;
			(unproxiedSetting as OnInputPhase)?.OnInputPhase(g, box);
		}
		finally
		{
			g.hoverKey = oldHoverKey;
			Input.currentGpKey = oldCurrentKey;
		}
	}
}