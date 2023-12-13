using FSPRO;

namespace Shockah.DuoArtifacts;

public sealed class ABubble : CardAction
{
	public int worldX;

	public override void Begin(G g, State s, Combat c)
	{
		if (!c.stuff.TryGetValue(worldX, out var @object))
			return;

		@object.bubbleShield = true;
		Audio.Play(Event.Status_PowerUp);
	}
}
