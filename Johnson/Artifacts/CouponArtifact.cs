using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class CouponArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Coupon", new()
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

	public override List<Tooltip>? GetExtraTooltips()
		=> [new TTGlossary("cardtrait.discount", 1)];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn || combat.turn != 1)
			return;

		combat.Queue([
			new ADelay(),
			new ACardSelect
			{
				browseAction = new DiscountBrowseAction { Amount = -1 },
				browseSource = CardBrowse.Source.Deck,
				artifactPulse = Key()
			}
		]);
	}
}
