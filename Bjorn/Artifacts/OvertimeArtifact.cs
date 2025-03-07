using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class OvertimeArtifact : Artifact, IRegisterable, IBjornApi.IHook
{
	private const int Threshold = 5;
	
	[JsonProperty]
	private int AnalyzedCards;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Overtime", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Overtime.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Overtime", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Overtime", "description"]).Localize
		});
	}

	public override int? GetDisplayNumber(State s)
		=> AnalyzedCards;

	public override List<Tooltip> GetExtraTooltips()
		=> AnalyzeManager.GetAnalyzeTooltips(MG.inst.g?.state ?? DB.fakeState);

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		AnalyzedCards = 0;
	}

	public void OnCardsAnalyzed(IBjornApi.IHook.IOnCardsAnalyzedArgs args)
	{
		foreach (var card in args.Cards)
		{
			AnalyzedCards++;
			if (AnalyzedCards < Threshold)
				continue;

			AnalyzedCards -= Threshold;
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(args.State, card, AnalyzeManager.AnalyzedTrait, false, false);
			if (args.Permanent)
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(args.State, card, AnalyzeManager.AnalyzedTrait, false, true);
			Pulse();
		}
	}
}