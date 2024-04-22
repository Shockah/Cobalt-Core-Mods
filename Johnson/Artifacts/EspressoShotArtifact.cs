using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class EspressoShotArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("EspressoShot", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/EspressoShot.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "EspressoShot", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "EspressoShot", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new List<Tooltip> { new TTCard { card = new BurnOutCard() } }
			.Concat(new BurnOutCard().GetAllTooltips(MG.inst.g, DB.fakeState))
			.ToList();

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn || combat.turn != 1)
			return;

		combat.Queue(new AAddCard
		{
			destination = CardDestination.Hand,
			card = new BurnOutCard(),
			artifactPulse = Key()
		});
	}
}
