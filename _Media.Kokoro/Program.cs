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

canvas.DrawModHeader(font, "Kokoro", "by Shockah", SKTextAlign.Right);

using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(Path.Combine(GetRootPath(), "..", "Kokoro", "Banner.png"));
data.SaveTo(stream);

static string GetRootPath([CallerFilePath] string path = "")
	=> Path.Combine(path, "..");