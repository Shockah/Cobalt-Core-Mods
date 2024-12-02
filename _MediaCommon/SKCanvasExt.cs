using SkiaSharp;
using System.Collections.Generic;

namespace Shockah.Media;

public static class SKCanvasExt
{
	public static void DrawOutlinedText(this SKCanvas canvas, string text, float x, float y, SKPaint paint, IEnumerable<(SKColor Color, float Width)> outlines)
	{
		var oldStyle = paint.Style;
		var oldColor = paint.Color;
		var oldStrokeWidth = paint.StrokeWidth;
		
		paint.Style = SKPaintStyle.Stroke;

		foreach (var outline in outlines)
		{
			paint.Color = outline.Color;
			paint.StrokeWidth = outline.Width;
			canvas.DrawText(text, x, y, paint);
		}
		
		paint.Style = oldStyle;
		paint.Color = oldColor;
		paint.StrokeWidth = oldStrokeWidth;
		canvas.DrawText(text, x, y, paint);
	}
}