using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class AgniGeneratorOverloadArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("AgniGeneratorOverload", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Agni/Artifact/GeneratorOverload.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "artifact", "GeneratorOverload", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Agni", "artifact", "GeneratorOverload", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("cardtrait.unplayable"),
			new TTGlossary("cardtrait.exhaust"),
		];
}