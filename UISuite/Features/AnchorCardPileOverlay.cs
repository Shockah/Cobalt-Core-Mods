using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.UISuite;

internal sealed class AnchorCardPileOverlay : IRegisterable
{
	private static ISpriteEntry AnchorOverlaySprite = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AnchorOverlaySprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/AnchorOverlay.png"));
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDeck)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDeck_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDiscard)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDiscard_Postfix))
		);
	}

	private static void RenderAnchorOverlayIfNeeded(G g, UIKey uiKey, List<Card> cards)
	{
		if (!cards.Any(card => card is TrashAnchor))
			return;
		if (g.boxes.FirstOrDefault(b => b.key == uiKey) is not { } box)
			return;
		
		var texture = SpriteLoader.Get(AnchorOverlaySprite.Sprite)!;
		Draw.Sprite(texture, box.rect.x2 - texture.Width + 2, box.rect.y - 2);
	}

	private static void Combat_RenderDeck_Postfix(G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		RenderAnchorOverlayIfNeeded(g, StableUK.combat_deck, g.state.deck);
	}

	private static void Combat_RenderDiscard_Postfix(Combat __instance, G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		RenderAnchorOverlayIfNeeded(g, StableUK.combat_discard, __instance.discard);
	}
}