﻿using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class FourDChessArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("FourDChess", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/FourDChess.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "FourDChess", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "FourDChess", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(RelativityManager.RelativityStatus.Status, 1);

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = RelativityManager.RelativityStatus.Status,
			statusAmount = 1,
			artifactPulse = Key(),
		});
	}
}