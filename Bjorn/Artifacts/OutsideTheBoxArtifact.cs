using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class OutsideTheBoxArtifact : Artifact, IRegisterable, IBjornApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("OutsideTheBox", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/OutsideTheBox.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "OutsideTheBox", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "OutsideTheBox", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. AnalyzeManager.GetAnalyzeTooltips(DB.fakeState),
			new TTGlossary("cardtrait.temporary"),
		];

	public bool CanAnalyze(IBjornApi.IHook.ICanAnalyzeArgs args)
	{
		var traitStates = ModEntry.Instance.Helper.Content.Cards.GetAllCardTraits(args.State, args.Card);

		if (!traitStates.TryGetValue(ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait, out var temporaryState) || !temporaryState.IsActive)
			return false;
		if (traitStates.TryGetValue(AnalyzeManager.AnalyzedTrait, out var analyzedState) && analyzedState.IsActive)
			return false;
		
		return true;
	}
}