using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MuscleMemoryArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("MuscleMemory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/MuscleMemory.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "MuscleMemory", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new OnDiscardManager.TriggerAction { Action = new ADummyAction() }.GetTooltips(DB.fakeState);
}