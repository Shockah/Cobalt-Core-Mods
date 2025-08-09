using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtRotorGreaseArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BreadnaughtRotorGrease", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.Boss],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Breadnaught/Artifact/RotorGrease.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "RotorGrease", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "RotorGrease", "description"]).Localize
		});
	}
}