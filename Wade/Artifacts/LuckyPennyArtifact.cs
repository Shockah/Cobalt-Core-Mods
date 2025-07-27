using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Wade;

internal sealed class LuckyPennyArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("LuckyPenny", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.WadeDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/LuckyPenny.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LuckyPenny", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LuckyPenny", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Odds.OddsStatus.Status, 1),
			.. StatusMeta.GetTooltips(Status.tempShield, 1),
		];

	public override void OnTurnEnd(State state, Combat combat)
	{
		if (state.ship.Get(Odds.OddsStatus.Status) < 0)
			combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 1, artifactPulse = Key() });
	}
}