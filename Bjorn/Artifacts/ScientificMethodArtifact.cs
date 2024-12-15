using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class ScientificMethodArtifact : Artifact, IRegisterable, IBjornApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("ScientificMethod", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/ScientificMethod.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ScientificMethod", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "ScientificMethod", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. AnalyzeManager.GetAnalyzeTooltips(DB.fakeState),
			.. new SmartShieldAction { Amount = 1 }.GetTooltips(DB.fakeState),
		];

	public void OnCardsAnalyzed(IBjornApi.IHook.IOnCardsAnalyzedArgs args)
		=> args.Combat.QueueImmediate(new SmartShieldAction { Amount = args.Cards.Count });
}