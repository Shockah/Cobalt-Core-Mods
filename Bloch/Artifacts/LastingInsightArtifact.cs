using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class LastingInsightArtifact : Artifact, IRegisterable, IBlochHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("LastingInsight", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/LastingInsight.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LastingInsight", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LastingInsight", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(AuraManager.InsightStatus.Status, Math.Max(MG.inst.g.state.ship.Get(AuraManager.InsightStatus.Status), 1));

	public void OnScryResult(State state, Combat combat, IReadOnlyList<Card> presentedCards, IReadOnlyList<Card> discardedCards, bool fromInsight)
	{
		if (!fromInsight)
			return;

		var keptCards = presentedCards.Where(card => !discardedCards.Any(card2 => card.uuid == card2.uuid)).ToList();
		if (keptCards.Count == 0)
			return;

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = AuraManager.InsightStatus.Status,
			statusAmount = keptCards.Count,
			artifactPulse = Key()
		});
	}
}