using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Johnson;

internal sealed class CouponArtifact : Artifact, IRegisterable
{
	internal static IArtifactEntry Entry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Entry = helper.Content.Artifacts.RegisterArtifact("Coupon", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Coupon.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Coupon", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Coupon", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [new TTGlossary("cardtrait.discount", 1)];

	public override void OnPlayerDeckShuffle(State state, Combat combat)
	{
		base.OnPlayerDeckShuffle(state, combat);

		var applicableCards = state.deck
			.Where(card => card.GetCurrentCost(state) > 0);

		if (applicableCards.Shuffle(state.rngActions).FirstOrDefault() is not { } card)
			return;

		Pulse();
		card.discount -= 1;
		state.storyVars.SetDiscounted(true);
		combat.QueueImmediate(new ADummyAction()); // force dialogue
	}
}
