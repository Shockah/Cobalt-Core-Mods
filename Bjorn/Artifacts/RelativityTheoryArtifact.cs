using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class RelativityTheoryArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("RelativityTheory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/RelativityTheory.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "RelativityTheory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "RelativityTheory", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.evade, 1),
			.. StatusMeta.GetTooltips(Status.droneShift, 1),
			.. StatusMeta.GetTooltips(RelativityManager.RelativityStatus.Status, 1),
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat is { isPlayerTurn: true, turn: 1 })
			combat.Queue(new Action { LateArtifactPulse = Key() });
	}

	private sealed class Action : CardAction
	{
		public required string LateArtifactPulse;
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var amount = s.ship.Get(Status.evade) + s.ship.Get(Status.droneShift);
			s.ship.Set(Status.evade, 0);
			s.ship.Set(Status.droneShift, 0);
			
			c.QueueImmediate([
				new AStatus
				{
					targetPlayer = true,
					status = RelativityManager.RelativityStatus.Status,
					statusAmount = amount,
					artifactPulse = LateArtifactPulse,
				},
			]);
		}
	}
}