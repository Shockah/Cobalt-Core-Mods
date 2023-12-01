namespace Shockah.Rerolls;

internal static class I18n
{
	public static string ArtifactName => "Rerolls";
	public static string ArtifactTooltip => "Allows you to reroll <c=artifact>artifact</c> or <c=card>card</c> rewards.";

	public static string GetArtifactCountTooltip(int rerollsLeft)
	{
		string extraTooltip = "Clearing a zone grants an extra reroll.";
		return rerollsLeft switch
		{
			0 => $"You have no rerolls left.\n{extraTooltip}",
			1 => $"You can reroll <c=boldPink>1</c> more time.\n{extraTooltip}",
			_ => $"You can reroll <c=boldPink>{rerollsLeft}</c> more times.\n{extraTooltip}",
		};
	}

	public static string RerollButton => "REROLL";
	public static string ShopOption => "Grant an extra reroll";
}
