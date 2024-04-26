using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class WingsOfNightArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("WingsOfNight", new()
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

	public override List<Tooltip>? GetExtraTooltips()
		=>
		[
			new TTCard
			{
				card = new BatFormCard
				{
					upgrade = Upgrade.A,
					temporaryOverride = true,
					exhaustOverride = true
				}
			},
			..StatusMeta.GetTooltips(Status.evade, 1),
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		if (!combat.isPlayerTurn)
			return;

		combat.QueueImmediate([
			new AAddCard
			{
				card = new BatFormCard
				{
					upgrade = Upgrade.A,
					temporaryOverride = true,
					exhaustOverride = true
				},
				destination = CardDestination.Hand,
				artifactPulse = Key()
			},
			new AStatus
			{
				targetPlayer = true,
				status = Status.evade,
				statusAmount = -1
			}
		]);
	}
}