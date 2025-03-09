using System.Linq;
using FSPRO;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerToggleBaysAction : CardAction
{
	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0.3;
			
		if (s.ship.key != KeplerShip.ShipEntry.UniqueName)
		{
			timer = 0;
			return;
		}

		var parts = s.ship.parts
			.Where(p => p.type == PType.missiles)
			.Where(p => p.skin == KeplerShip.LeftBayEntry.UniqueName || p.skin == KeplerShip.RightBayEntry.UniqueName)
			.ToList();
		var partToActivate = parts.FirstOrDefault(p => !p.active) ?? parts.FirstOrDefault();

		foreach (var part in parts)
			part.active = part == partToActivate;
		Audio.Play(Event.TogglePart);
	}
}