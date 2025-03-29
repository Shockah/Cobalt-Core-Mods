using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Shockah.Media;
using SkiaSharp;

// ReSharper disable ConvertToUsingDeclaration

var imageInfo = new SKImageInfo(1920, 1080);
var surface = SKSurface.Create(imageInfo);
var canvas = surface.Canvas!;

var font = SKTypeface.FromFile(Path.Combine(GetRootPath(), "..", "_MediaCommon", "assets", "Expressway Rg Bold.ttf"))!;
var background = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", "Background.png"));
var portrait = SKImage.FromEncodedData(Path.Combine(GetRootPath(), "..", "Bjorn", "assets", "Character", "Neutral", "0.png"));
var artifactNames = new List<string> { "OutsideTheBox", "Overtime", "ScientificMethod", "SideProjects", "SpecialRelativity", "Synchrotron" };
var artifactImages = artifactNames.Select(n => SKImage.FromEncodedData(Path.Combine(GetRootPath(), "..", "Bjorn", "assets", "Artifacts", $"{n}.png"))).ToList();
var cardImages = Enumerable.Range(1, 3).Select(i => SKImage.FromEncodedData(Path.Combine(GetRootPath(), "assets", $"Card{i}.png"))).ToList();

canvas.Clear(SKColors.Empty);
canvas.DrawImage(background, new SKRect(0, 0, imageInfo.Width, imageInfo.Height));

using (var paint = new SKPaint())
{
	paint.Color = SKColors.White;
	var width = portrait.Width * 12;
	var height = portrait.Height * 12;
	var x = 32;
	var y = -24f;
	canvas.DrawImage(portrait, new SKRect(x, imageInfo.Height - y - height, x + width, imageInfo.Height - y), paint);
}

var artifactRowCount = (int)Math.Ceiling(Math.Sqrt(artifactImages.Count));

var artifactRows = new int[artifactRowCount];
foreach (var index in GetSquareFillIndexes(artifactRowCount).Take(artifactImages.Count))
	artifactRows[index]++;

var artifactImageGroups = GetGroupedImages(artifactImages, artifactRows);

IEnumerable<int> GetSquareFillIndexes(int size)
{
	var center = size / 2;

	for (var currentSize = 1; currentSize <= size; currentSize++)
	{
		yield return center;
		for (var offset = 1; offset < size; offset++)
		{
			if (center + offset < size)
				yield return center + offset;
			if (center - offset >= 0)
				yield return center - offset;
		}
	}
}

List<List<SKImage>> GetGroupedImages(IEnumerable<SKImage> images, int[] groups)
{
	var result = new List<List<SKImage>>();
	var rowIndex = 0;
	var currentRow = new List<SKImage>();

	foreach (var image in images)
	{
		while (groups[rowIndex] == 0)
			rowIndex++;
		
		currentRow.Add(image);

		if (currentRow.Count >= groups[rowIndex])
		{
			result.Add(currentRow);
			currentRow = [];
			rowIndex++;
		}
	}

	if (currentRow.Count != 0)
		result.Add(currentRow);
	
	return result;
}

using (var paint = new SKPaint())
{
	paint.Color = SKColors.White;

	const int artifactScale = 10;
	const int originX = 240;
	const int originY = 48;
	const float spacing = 2.5f;
	var realSpacing = (artifactImages[0].Width + spacing) * artifactScale;

	for (var cellY = 0; cellY < artifactImageGroups.Count; cellY++)
	{
		for (var cellX = 0; cellX < artifactImageGroups[cellY].Count; cellX++)
		{
			var artifactImage = artifactImageGroups[cellY][cellX];
			var imageWidth = artifactImage.Width * artifactScale;
			var imageHeight = artifactImage.Height * artifactScale;

			var imageLeft = originX + cellX * realSpacing;
			var imageTop = originY + cellY * realSpacing;
			canvas.DrawImage(artifactImage, new SKRect(imageLeft, imageTop, imageLeft + imageWidth, imageTop + imageHeight), paint);
		}
	}
}

using (var paint = new SKPaint())
{
	paint.Color = SKColors.White;

	const float xOffset = 32;
	const float yOffset = 32;
	const float cardScale = 1.8f;
	const float xSpacing = -0.1f;
	const float ySpacing = 0.15f;
	var realXSpacing = cardImages[0].Width * cardScale * (1 + xSpacing);
	var realYSpacing = cardImages[0].Height * cardScale * ySpacing;

	for (var i = cardImages.Count - 1; i >= 0; i--)
	{
		var cardImage = cardImages[i];
		var imageWidth = cardImage.Width * cardScale;
		var imageHeight = cardImage.Height * cardScale;
		
		var imageLeft = imageInfo.Width - imageWidth - xOffset - realXSpacing * i;
		var imageTop = yOffset + realYSpacing * i;
		canvas.DrawImage(cardImage, new SKRect(imageLeft, imageTop, imageLeft + imageWidth, imageTop + imageHeight), paint);
	}
}

canvas.DrawModHeader(font, "Bjorn", "by Shockah, Soggoru Waffle", SKTextAlign.Right);

using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = File.OpenWrite(Path.Combine(GetRootPath(), "..", "Bjorn", "Banner.png"));
data.SaveTo(stream);

static string GetRootPath([CallerFilePath] string path = "")
	=> Path.Combine(path, "..");