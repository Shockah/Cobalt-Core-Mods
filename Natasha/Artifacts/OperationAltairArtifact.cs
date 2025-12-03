using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class OperationAltairArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("OperationAltair", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/OperationAltair.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "OperationAltair", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "OperationAltair", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(MG.inst.g.state, null) ?? [],
			new TTGlossary("cardtrait.exhaust"),
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn != 1 || !combat.isPlayerTurn)
			return;
		
		combat.Queue(ModEntry.Instance.KokoroApi.Limited.ModifyCardSelect(new ACardSelect
		{
			browseAction = new BrowseAction { ArtifactPulseLate = Key() },
			browseSource = CardBrowse.Source.Deck,
			allowCancel = true,
			filterExhaust = false,
		}).SetFilterLimited(false).AsCardAction);
	}

	private sealed class BrowseAction : CardAction
	{
		public string? ArtifactPulseLate;
		
		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["artifact", "OperationAltair", "chooseUiTitle"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			if (selectedCard is null)
				return;
			
			if (ArtifactPulseLate is not null)
				s.EnumerateAllArtifacts().FirstOrDefault(a => a.Key() == ArtifactPulseLate)?.Pulse();
			
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, ModEntry.Instance.KokoroApi.Limited.Trait, true, permanent: true);
		}
	}
}