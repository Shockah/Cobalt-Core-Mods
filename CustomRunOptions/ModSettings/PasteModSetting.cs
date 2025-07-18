using System;
using System.Collections.Generic;
using daisyowl.text;
using FSPRO;
using Nickel.ModSettings;
using TextCopy;

namespace Shockah.CustomRunOptions;

internal sealed class PasteModSetting : IModSettingsApi.IModSetting, OnMouseDown
{
	public UIKey Key { get; private set; }
	public event IModSettingsApi.OnMenuOpen? OnMenuOpen;
	public event IModSettingsApi.OnMenuClose? OnMenuClose;

	public required Func<string> Title { get; set; }
	public required Func<string?> ValueGetter { get; set; }
	public required Action<string?> ValueSetter { get; set; }
	public Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

	private UIKey ButtonKey;

	public PasteModSetting()
	{
		this.OnMenuOpen += (_, _) =>
		{
			if (this.Key == 0)
				this.Key = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.ButtonKey == 0)
				this.ButtonKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		};
	}

	~PasteModSetting()
	{
		if (this.Key != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.Key.k);
		if (this.ButtonKey != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.ButtonKey.k);
	}

	public void RaiseOnMenuOpen(G g, IModSettingsApi.IModSettingsRoute route)
		=> this.OnMenuOpen?.Invoke(g, route);

	public void RaiseOnMenuClose(G g)
		=> this.OnMenuClose?.Invoke(g);

	public Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
			box.autoFocus = true;

			var isHover = box.IsHover() || g.hoverKey == this.ButtonKey;
			if (isHover)
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);

			var value = ValueGetter();
			if (string.IsNullOrEmpty(value))
				value = null;

			Draw.Text(Title(), box.rect.x + 10, box.rect.y + 5, DB.thicket, isHover ? Colors.textChoiceHoverActive : Colors.textMain);
			if (value is not null)
				Draw.Text(value, box.rect.x2 - 76, box.rect.y + 5, DB.thicket, isHover ? Colors.textChoiceHoverActive : Colors.textMain, align: TAlign.Right);

			SharedArt.ButtonText(
				g, new Vec(box.rect.w - 10 - 60, -3),
				this.ButtonKey,
				ModEntry.Instance.Localizations.Localize(["options", nameof(SeedCustomRunOption), value is null ? "paste" : "clear"]),
				showAsPressed: Input.gamepadIsActiveInput && isHover,
				onMouseDown: this
			);

			if ((box.IsHover() || g.hoverKey == this.ButtonKey) && this.Tooltips is { } tooltips)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
		}

		return new(box.rect.w, 20);
	}

	public void OnMouseDown(G g, Box b)
	{
		if ((!Input.gamepadIsActiveInput || b.key != Key) && b.key != ButtonKey)
			return;
		
		var value = ValueGetter();
		if (string.IsNullOrEmpty(value))
			value = null;
		
		Audio.Play(Event.Click);
		ValueSetter(value is null ? ClipboardService.GetText() : null);
	}
}