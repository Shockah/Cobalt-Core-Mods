namespace Shockah.Soggins;

public interface IAppleArtifactApi
{
	public delegate CardAction SingleActionProvider(State state);
	public void SetPaletteAction(Deck deck, SingleActionProvider action, Tooltip description);
}