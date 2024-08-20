using System.Text.RegularExpressions;

namespace Shockah.Shared;

internal static partial class TextParserExt
{
	private static readonly Regex ColorTagStartRegex = MyRegex();
	private static readonly string ColorTagEndPattern = "</c>";

	// i know this is very naive, but it works good enough for the use cases
	public static string StripColorsFromText(string text)
		=> ColorTagStartRegex.Replace(text, "").Replace(ColorTagEndPattern, "");
	
	[GeneratedRegex(@"\<c\=.+?\>")]
	private static partial Regex MyRegex();
}