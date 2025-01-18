using System;
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

	public static void DrawModHeader(this SKCanvas canvas, SKTypeface font, string title, string subtitle, SKTextAlign alignment)
	{
		var x = alignment switch
		{
			SKTextAlign.Left => 24,
			SKTextAlign.Center => canvas.DeviceClipBounds.Width / 2,
			SKTextAlign.Right => canvas.DeviceClipBounds.Width - 24,
			_ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null)
		};
		
		using (var paint = new SKPaint())
		{
			paint.Typeface = font;
			paint.TextSize = 144;
			paint.TextAlign = alignment;
			paint.IsAntialias = true;
			paint.Color = SKColors.White;
			canvas.DrawOutlinedText(title, x, canvas.DeviceClipBounds.Height - 110, paint, [
				(SKColors.Black.WithAlpha(127), 8),
				(SKColors.Black, 4),
			]);
		}

		using (var paint = new SKPaint())
		{
			paint.Typeface = font;
			paint.TextSize = 72;
			paint.TextAlign = alignment;
			paint.IsAntialias = true;
			paint.Color = SKColors.White;
			canvas.DrawOutlinedText(subtitle, x, canvas.DeviceClipBounds.Height - 30, paint, [
				(SKColors.Black.WithAlpha(127), 8),
				(SKColors.Black, 4),
			]);
		}
	}
}