using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dyna;

internal sealed class BlastPowderArtifact : Artifact, IRegisterable, IDynaHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BlastPowder", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/BlastPowder.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BlastPowder", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BlastPowder", "description"]).Localize,
		});
	}

	public void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX)
		=> ship.NormalDamage(state, combat, 1, worldX);
}