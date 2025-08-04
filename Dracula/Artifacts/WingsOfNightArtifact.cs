using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class WingsOfNightArtifact : Artifact, IRegisterable
{
	internal static IArtifactEntry Entry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Entry = helper.Content.Artifacts.RegisterArtifact("WingsOfNight", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/WingsOfNight.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "WingsOfNight", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "WingsOfNight", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTCard { card = new BatFormCard { upgrade = Upgrade.A, temporaryOverride = true, exhaustOverride = true } },
			.. StatusMeta.GetTooltips(Status.evade, 1),
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		if (!combat.isPlayerTurn)
			return;

		combat.Queue(new Action { PassthroughArtifactPulse = Key() });
	}

	private sealed class Action : CardAction
	{
		public required string PassthroughArtifactPulse;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.ship.Get(Status.evade) <= 0)
				return;

			c.QueueImmediate([
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = -1, timer = 0 },
				new AAddCard
				{
					card = new BatFormCard { upgrade = Upgrade.A, temporaryOverride = true, exhaustOverride = true },
					destination = CardDestination.Hand,
					artifactPulse = PassthroughArtifactPulse,
				},
			]);
		}
	}
}