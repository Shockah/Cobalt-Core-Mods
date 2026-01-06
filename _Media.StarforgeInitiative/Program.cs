using System.IO;
using System.Runtime.CompilerServices;
using Shockah.Media;
using SkiaSharp;

// ReSharper disable ConvertToUsingDeclaration

const int shipScale = 6;

var imageInfo = new SKImageInfo(1920, 1080);
var surface = SKSurface.Create(imageInfo);
var canvas = surface.Canvas!;

var font = SKTypeface.FromFile(Path.Combine(GetRootPath(), "..", "_MediaCommon", "assets", "Expressway Rg Bold.ttf"))!;
var background = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "Background.jpg"));
var breadnaught = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "Breadnaught.png"));
var kepler = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "Kepler.png"));
var nemesis = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "Nemesis.png"));
var agni = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "Agni.png"));

canvas.Clear(SKColors.Empty);
canvas.DrawImage(background, new SKRect(0, 0, imageInfo.Width, imageInfo.Height));

using (var paint = new SKPaint())
{
	paint.Color = SKColors.White;

	{
		var sprite = breadnaught;
		var width = sprite.Width * shipScale;
		var height = sprite.Height * shipScale;
		var originX = imageInfo.Width / 2 - (int)(imageInfo.Width * 0.31);
		var originY = imageInfo.Height / 2 + (int)(imageInfo.Height * 0.28);
		canvas.DrawImage(sprite, new SKRect(originX - width / 2, originY - height / 2, originX + width / 2, originY + height / 2), paint);
	}
	
	{
		var sprite = kepler;
		var width = sprite.Width * shipScale;
		var height = sprite.Height * shipScale;
		var originX = imageInfo.Width / 2 - (int)(imageInfo.Width * 0.16);
		var originY = imageInfo.Height / 2 - (int)(imageInfo.Height * 0.01);
		canvas.DrawImage(sprite, new SKRect(originX - width / 2, originY - height / 2, originX + width / 2, originY + height / 2), paint);
	}
	
	{
		var sprite = agni;
		var width = sprite.Width * shipScale;
		var height = sprite.Height * shipScale;
		var originX = imageInfo.Width / 2 - (int)(imageInfo.Width * 0.01);
		var originY = imageInfo.Height / 2 - (int)(imageInfo.Height * 0.30);
		canvas.DrawImage(sprite, new SKRect(originX - width / 2, originY - height / 2, originX + width / 2, originY + height / 2), paint);
	}
	
	{
		var sprite = nemesis;
		var width = sprite.Width * shipScale;
		var height = sprite.Height * shipScale;
		var originX = imageInfo.Width / 2 + (int)(imageInfo.Width * 0.23);
		var originY = imageInfo.Height / 2 - (int)(imageInfo.Height * 0.0);
		canvas.DrawImage(sprite, new SKRect(originX - width / 2, originY - height / 2, originX + width / 2, originY + height / 2), paint);
	}
}

canvas.DrawModHeader(font, "Starforge Initiative", "by Arin, Shockah", SKTextAlign.Right);

using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(Path.Combine(GetRootPath(), "..", "StarforgeInitiative", "Banner.png"));
data.SaveTo(stream);

static string GetRootPath([CallerFilePath] string path = "")
	=> Path.Combine(path, "..");