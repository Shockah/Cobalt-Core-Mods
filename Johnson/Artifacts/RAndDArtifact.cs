using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class RAndDArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("RAndD", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/RAndD.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "RAndD", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "RAndD", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [new TTGlossary("cardtrait.temporary")];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn)
			return;

		combat.Queue([
			new ADelay(),
			new ACardOffering
			{
				amount = 2,
				makeAllCardsTemporary = true,
				overrideUpgradeChances = false,
				canSkip = false,
				inCombat = true,
				artifactPulse = Key()
			}
		]);
	}
}
