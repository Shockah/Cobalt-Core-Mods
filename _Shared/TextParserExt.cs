using System.Text.RegularExpressions;

namespace Shockah.Shared;

public static class TextParserExt
{
	private static readonly Regex ColorTagStartRegex = new("\\<c\\=.+?\\>");
	private static readonly string ColorTagEndPattern = "</c>";

	// i know this is very naive, but it works good enough for the use cases
	public static string StripColorsFromText(string text)
		=> ColorTagStartRegex.Replace(text, "").Replace(ColorTagEndPattern, "");
}