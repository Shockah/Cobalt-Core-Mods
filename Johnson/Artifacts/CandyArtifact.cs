using Nanoray.PluginManager;
using Nickel;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class CandyArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Candy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Candy.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Candy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Candy", "description"]).Localize
		});
	}

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);

		state.GetCurrentQueue().InsertRange(0, [
			new ACardSelect
			{
				browseAction = new SpecificUpgradeBrowseAction { Upgrade = Upgrade.A },
				browseSource = CardBrowse.Source.Deck,
				filterTemporary = false
			}.SetFilterPermanentlyUpgraded(false),
			new ACardSelect
			{
				browseAction = new SpecificUpgradeBrowseAction { Upgrade = Upgrade.B },
				browseSource = CardBrowse.Source.Deck,
				filterTemporary = false
			}.SetFilterPermanentlyUpgraded(false),
		]);
	}
}
