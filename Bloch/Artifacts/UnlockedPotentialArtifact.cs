using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class UnlockedPotentialArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("UnlockedPotential", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/UnlockedPotential.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnlockedPotential", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "UnlockedPotential", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new OncePerTurnManager.TriggerAction { Action = new ADummyAction() }.GetTooltips(DB.fakeState);
}