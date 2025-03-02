using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.CatExpansion;

internal sealed class HotReloadArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("HotReload", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.catartifact,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!),
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/HotReload.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "HotReload", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "HotReload", "description"]).Localize
		});
	}

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.ship.baseEnergy++;

		state.GetCurrentQueue().InsertRange(0, [
			new AAddCard { card = new CannonColorless(), amount = 3 },
			new AAddCard { card = new BasicShieldColorless() },
		]);
	}

	public override void OnRemoveArtifact(State state)
	{
		base.OnRemoveArtifact(state);
		state.ship.baseEnergy--;
	}
}