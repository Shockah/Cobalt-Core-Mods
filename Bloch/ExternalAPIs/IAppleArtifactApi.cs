namespace Shockah.Bloch;

public interface IAppleArtifactApi
{
	public delegate CardAction SingleActionProvider(State state);
	public void SetPaletteAction(Deck deck, SingleActionProvider action, Tooltip description);
}