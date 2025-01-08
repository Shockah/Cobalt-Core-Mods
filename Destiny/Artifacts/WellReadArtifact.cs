using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class WellReadArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("WellRead", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DestinyDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/WellRead.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "WellRead", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "WellRead", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(MagicFind.MagicFindStatus.Status, 3);

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn != 1)
			return;
		combat.Queue(new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 3, artifactPulse = Key() });
	}
}