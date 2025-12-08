using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class VulcanUpgradedCapacitorsArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("VulcanUpgradedCapacitors", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Boss],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Vulcan/Artifact/UpgradedCapacitors.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Vulcan", "artifact", "UpgradedCapacitors", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Vulcan", "artifact", "UpgradedCapacitors", "description"]).Localize
		});
	}
}