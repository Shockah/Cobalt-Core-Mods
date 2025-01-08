using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class ShardBankArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("ShardBank", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DestinyDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/ShardBank.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ShardBank", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ShardBank", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.shard, (MG.inst.g.state?.ship ?? DB.fakeState.ship).GetMaxShard()),
			.. StatusMeta.GetTooltips(MagicFind.MagicFindStatus.Status, 1),
		];
}