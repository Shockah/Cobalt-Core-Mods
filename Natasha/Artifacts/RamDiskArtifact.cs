using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class RamDiskArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("RamDisk", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/RamDisk.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "RamDisk", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "RamDisk", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			.. (Limited.Trait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? []),
			new TTGlossary("cardtrait.singleUse"),
		];
}