using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Wade;

internal sealed class FuzzyDiceArtifact : Artifact, IRegisterable, IWadeApi.IHook
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	public bool TriggeredThisTurn;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/FuzzyDice.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/FuzzyDiceInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("FuzzyDice", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.WadeDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "FuzzyDice", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "FuzzyDice", "description"]).Localize
		});
	}

	public override Spr GetSprite()
		=> TriggeredThisTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. new Odds.RollAction().GetTooltips(MG.inst?.g?.state ?? DB.fakeState),
			.. StatusMeta.GetTooltips(Status.tempShield, 1),
		];

	public override void OnTurnStart(State state, Combat combat)
		=> TriggeredThisTurn = false;

	public void OnOddsRoll(IWadeApi.IHook.IOnOddsRollsArgs args)
	{
		if (TriggeredThisTurn)
			return;
		if (args.IsTurnStart)
			return;

		TriggeredThisTurn = true;
		args.Combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 1, artifactPulse = Key() });
	}
}