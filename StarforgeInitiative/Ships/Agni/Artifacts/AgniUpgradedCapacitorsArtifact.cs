using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class AgniUpgradedCapacitorsArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("AgniUpgradedCapacitors", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Boss],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Agni/Artifact/UpgradedCapacitors.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "artifact", "UpgradedCapacitors", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "artifact", "UpgradedCapacitors", "description"]).Localize
		});
	}
}