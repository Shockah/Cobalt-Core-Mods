using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerBayV2Artifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("KeplerBayV2", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Boss],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Kepler/Artifact/BayV2.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "BayV2", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "artifact", "BayV2", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTCard { card = new KeplerSwarmModeCard() }];

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.GetCurrentQueue().QueueImmediate(new ALoseArtifact { artifactType = new KeplerBayArtifact().Key() });
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		
		if (!combat.hand.Any(card => card is KeplerSwarmModeCard))
			combat.Queue(new DelayedAction());

		var activeParts = state.ship.parts
			.Where(p => p.type == PType.missiles)
			.Where(p => p.skin == KeplerShip.LeftBayEntry.UniqueName || p.skin == KeplerShip.RightBayEntry.UniqueName)
			.Where(p => p.active);

		if (activeParts.Count() != 1)
			combat.Queue(new KeplerToggleBaysAction());
	}

	private sealed class DelayedAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			if (c.hand.Any(card => card is KeplerSwarmModeCard))
				return;
			c.Queue(new AAddCard { destination = CardDestination.Hand, card = new KeplerSwarmModeCard() });
		}
	}
}