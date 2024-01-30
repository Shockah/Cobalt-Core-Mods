using Nanoray.PluginManager;
using Nickel;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class BriefcaseArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Briefcase", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Briefcase.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Briefcase", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Briefcase", "description"]).Localize
		});
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn || combat.turn != 1)
			return;

		combat.Queue([
			new ADelay(),
			new ASpecificCardOffering
			{
				Destination = CardDestination.Hand,
				Cards = [
					new BulletPointCard(),
					new SlideTransitionCard(),
					new LeverageCard(),
					new BrainstormCard(),
				],
			}
		]);
	}
}
