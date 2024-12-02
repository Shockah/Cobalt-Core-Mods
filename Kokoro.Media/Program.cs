using Shockah.Media;
using SkiaSharp;
using System.IO;
using System.Runtime.CompilerServices;

// ReSharper disable ConvertToUsingDeclaration

var imageInfo = new SKImageInfo(1920, 1080);
var surface = SKSurface.Create(imageInfo);
var canvas = surface.Canvas!;

var font = SKTypeface.FromFile(Path.Combine(GetRootPath(), "..", "_MediaCommon", "assets", "Expressway Rg Bold.ttf"))!;
var background = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "BannerBackground.png"));

canvas.Clear(SKColors.Empty);
canvas.DrawImage(background, 0, 0);

using (var paint = new SKPaint())
{
	paint.Typeface = font;
	paint.TextSize = 144;
	paint.TextAlign = SKTextAlign.Right;
	paint.IsAntialias = true;
	paint.Color = SKColors.White;
	canvas.DrawOutlinedText("Kokoro", imageInfo.Width - 24, imageInfo.Height - 110, paint, [
		(SKColors.Black.WithAlpha(127), 8),
		(SKColors.Black, 4),
	]);
}

using (var paint = new SKPaint())
{
	paint.Typeface = font;
	paint.TextSize = 72;
	paint.TextAlign = SKTextAlign.Right;
	paint.IsAntialias = true;
	paint.Color = SKColors.White;
	canvas.DrawOutlinedText("by Shockah", imageInfo.Width - 24, imageInfo.Height - 30, paint, [
		(SKColors.Black.WithAlpha(127), 8),
		(SKColors.Black, 4),
	]);
}

using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(Path.Combine(GetRootPath(), "..", "Kokoro", "Banner.png"));
data.SaveTo(stream);

static string GetRootPath([CallerFilePath] string path = "")
	=> Path.Combine(path, "..");