using System.Collections.Generic;

namespace Shockah.Wormholes;

internal sealed class MapWormhole : MapNodeContents
{
	public Vec OtherWormholePosition;
	public bool IsFurther = false;

	public override void Render(G g, Vec v)
	{
		Draw.Sprite((Spr)ModEntry.Instance.WormholeSprite.Id!.Value, v.x, v.y);
	}

	public override List<Tooltip> GetTooltips(G g)
		=> [new TTText(I18n.WormholeTooltip)];

	public override Route MakeRoute(State s, Vec coord)
	{
		ModEntry.Instance.WormholeMode = IsFurther ? WormholeMode.Backward : WormholeMode.Forward;
		s.map.currentLocation = OtherWormholePosition;
		s.map.markers[OtherWormholePosition].wasVisited = true;
		return Dialogue.MakeDialogueRouteOrSkip(s, DB.story.QuickLookup(s, "BootSequenceDownside"), OnDone.map);
	}
}
