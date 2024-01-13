using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class WingsOfNightArtifact : Artifact, IDraculaArtifact
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("WingsOfNight", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/WingsOfNight.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "WingsOfNight", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "WingsOfNight", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new List<Tooltip>
		{
			new TTCard
			{
				card = new BatFormCard
				{
					temporaryOverride = true,
					exhaustOverride = true
				}
			}
		}
		.Concat(StatusMeta.GetTooltips(Status.evade, 1))
		.ToList();

	public override void OnTurnStart(State state, Combat combat)
	{
		if (!combat.isPlayerTurn)
			return;
		combat.QueueImmediate([
			new AAddCard
			{
				card = new BatFormCard
				{
					temporaryOverride = true,
					exhaustOverride = true
				},
				destination = CardDestination.Hand
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