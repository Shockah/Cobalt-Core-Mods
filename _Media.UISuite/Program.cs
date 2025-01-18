using Shockah.Media;
using SkiaSharp;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable ConvertToUsingDeclaration

var imageInfo = new SKImageInfo(1920, 1080);
var surface = SKSurface.Create(imageInfo);
var canvas = surface.Canvas!;

var font = SKTypeface.FromFile(Path.Combine(GetRootPath(), "..", "_MediaCommon", "assets", "Expressway Rg Bold.ttf"))!;
var screenshot1 = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "Screenshot1.jpg"));

canvas.Clear(SKColors.Empty);
canvas.DrawImage(screenshot1, 0, 0);

// using (var paint = new SKPaint())
// {
// 	paint.Shader = SKShader.CreateImage(screenshot1);
// 	DrawNormalizedVertices(
// 		SKVertexMode.TriangleStrip,
// 		[
// 			new(1f, 1f), new(0f, 1f), new(1f, 0.55f), new(0f, 0.55f),
// 			new(1f, 0.45f), new(0f, 0.45f)
// 		],
// 		[
// 			SKColors.White, SKColors.White, SKColors.White, SKColors.White,
// 			SKColors.White.WithAlpha(0), SKColors.White.WithAlpha(0),
// 		],
// 		paint
// 	);
// }

canvas.DrawModHeader(font, "UI Suite", "by Shockah", SKTextAlign.Right);

using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(Path.Combine(GetRootPath(), "..", "UISuite", "Banner.png"));
data.SaveTo(stream);

static string GetRootPath([CallerFilePath] string path = "")
	=> Path.Combine(path, "..");

void DrawNormalizedVertices(SKVertexMode vmode, SKPoint[] vertices, SKColor[] colors, SKPaint paint)
	=> canvas.DrawVertices(vmode, vertices.Select(p => new SKPoint(p.X * imageInfo.Width, p.Y * imageInfo.Height)).ToArray(), colors, paint);