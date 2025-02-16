using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class SideProjectsArtifact : Artifact, IRegisterable, IBjornApi.IHook
{
	public const int CardsToAnalyzePerProgress = 5;
	public const int MaxProgressPerCombat = 5;
	
	public int AnalyzedCards;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("SideProjects", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/SideProjects.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SideProjects", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SideProjects", "description"]).Localize
		});
	}

	public override int? GetDisplayNumber(State s)
		=> Math.Min(AnalyzedCards, CardsToAnalyzePerProgress * MaxProgressPerCombat);

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("cardtrait.temporary"),
			.. AnalyzeManager.GetAnalyzeTooltips(DB.fakeState),
			.. StatusMeta.GetTooltips(GadgetManager.GadgetStatus.Status, 1),
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		AnalyzedCards = 0;
	}

	public void OnCardsAnalyzed(IBjornApi.IHook.IOnCardsAnalyzedArgs args)
	{
		var oldProgressGained = Math.Min(AnalyzedCards / CardsToAnalyzePerProgress, MaxProgressPerCombat);
		AnalyzedCards += args.Cards.Count(card => !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait));
		var newProgressGained = Math.Min(AnalyzedCards / CardsToAnalyzePerProgress, MaxProgressPerCombat);
		
		if (newProgressGained != oldProgressGained)
			args.Combat.QueueImmediate(new AStatus
			{
				targetPlayer = true,
				status = GadgetManager.GadgetStatus.Status,
				statusAmount = newProgressGained - oldProgressGained,
				artifactPulse = Key(),
			});
	}
}