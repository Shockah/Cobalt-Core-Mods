using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Wade;

internal sealed class D4Artifact : Artifact, IRegisterable
{
	private const int Turns = 4;
	
	public int Counter;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("D4", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.WadeDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/D4.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "D4", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "D4", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTCard { card = new SpareDiceCard() }];

	public override int? GetDisplayNumber(State s)
		=> Counter;

	public override void OnTurnStart(State state, Combat combat)
	{
		Counter++;
		if (Counter < Turns)
			return;

		Counter = 0;
		combat.Queue(new AAddCard { destination = CardDestination.Hand, card = new SpareDiceCard(), artifactPulse = Key() });
	}
}