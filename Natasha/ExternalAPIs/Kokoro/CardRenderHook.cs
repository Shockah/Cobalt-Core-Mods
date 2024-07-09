using daisyowl.text;

namespace Shockah.Natasha;

public partial interface IKokoroApi
{
	void RegisterCardRenderHook(ICardRenderHook hook, double priority);
	void UnregisterCardRenderHook(ICardRenderHook hook);

	Font PinchCompactFont { get; }
}

public interface ICardRenderHook
{
	Font? ReplaceTextCardFont(G g, Card card) => null;
}