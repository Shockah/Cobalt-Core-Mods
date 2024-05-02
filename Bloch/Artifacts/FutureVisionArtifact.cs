using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class FutureVisionArtifact : Artifact, IRegisterable, IBlochHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("FutureVision", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/FutureVision.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "FutureVision", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "FutureVision", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			..new ScryAction { Amount = 1 }.GetTooltips(DB.fakeState),
			new TTGlossary("cardtrait.discount", 1)
		];

	public void OnScryResult(State state, Combat combat, IReadOnlyList<Card> presentedCards, IReadOnlyList<Card> discardedCards, bool fromInsight)
	{
		var keptCards = presentedCards.Where(card => !discardedCards.Any(card2 => card.uuid == card2.uuid)).ToList();
		if (keptCards.Count == 0)
			return;

		var discountableCards = keptCards.Where(card => card.GetCurrentCost(state) > 0).ToList();
		if (discountableCards.Count == 0)
			discountableCards = keptCards;

		var cardToDiscount = discountableCards.Count == 1 ? discountableCards[0] : discountableCards[state.rngActions.NextInt() % discountableCards.Count];
		cardToDiscount.discount--;
		Pulse();
	}
}