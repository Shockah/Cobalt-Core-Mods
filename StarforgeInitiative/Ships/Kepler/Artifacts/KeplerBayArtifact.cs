using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerBayArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("KeplerBay", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Kepler/Artifact/Bay.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "Bay", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "Bay", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTCard { card = new KeplerToggleBayCard() }];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		
		if (!combat.hand.Any(card => card is KeplerToggleBayCard))
			combat.Queue(new DelayedAction());
	}

	private sealed class DelayedAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			if (c.hand.Any(card => card is KeplerToggleBayCard))
				return;
			c.Queue(new AAddCard { destination = CardDestination.Hand, card = new KeplerToggleBayCard() });
		}
	}
}