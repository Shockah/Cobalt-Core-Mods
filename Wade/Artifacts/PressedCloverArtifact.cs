using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Wade;

internal sealed class PressedCloverArtifact : Artifact, IRegisterable, IWadeApi.IHook
{
	private const int TimesPerTurn = 2;
	
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;
	
	public int Counter;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/PressedClover.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifact/PressedCloverInactive.png"));
		
		helper.Content.Artifacts.RegisterArtifact("PressedClover", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.WadeDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PressedClover", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "PressedClover", "description"]).Localize
		});
	}

	public override Spr GetSprite()
		=> Counter >= TimesPerTurn ? InactiveSprite.Sprite : ActiveSprite.Sprite;

	public override int? GetDisplayNumber(State s)
		=> Math.Clamp(Counter, 0, TimesPerTurn);

	public override List<Tooltip> GetExtraTooltips()
		=> new Odds.RollAction().GetTooltips(MG.inst?.g?.state ?? DB.fakeState);

	public override void OnTurnStart(State state, Combat combat)
		=> Counter = 0;

	public void OnOddsRoll(IWadeApi.IHook.IOnOddsRollsArgs args)
	{
		if (Counter >= TimesPerTurn)
			return;
		if (args.IsTurnStart)
			return;

		Counter++;
		args.Combat.QueueImmediate(new AEnergy { changeAmount = 1, artifactPulse = Key() });
	}
}