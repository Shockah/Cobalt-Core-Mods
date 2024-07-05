namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public interface IActionApi
	{
		CardAction MakeHidden(CardAction action, bool showTooltips = false);
	}
}