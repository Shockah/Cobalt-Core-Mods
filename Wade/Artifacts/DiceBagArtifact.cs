using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Wade;

internal sealed class DiceBagArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("DiceBag", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.WadeDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/DiceBag.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "DiceBag", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "DiceBag", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTCard { card = new SpareDiceCard() }];

	public override void OnCombatStart(State state, Combat combat)
	{
		combat.Queue(new AAddCard { destination = CardDestination.Hand, card = new SpareDiceCard(), amount = 2, artifactPulse = Key() });
	}
}