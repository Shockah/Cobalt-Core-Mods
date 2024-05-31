using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class HardHatArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("HardHat", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/HardHat.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "HardHat", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "HardHat", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.energyLessNextTurn, Math.Max(MG.inst.g.state.ship.Get(Status.energyLessNextTurn), 1)),
			.. StatusMeta.GetTooltips(ModEntry.Instance.KokoroApi.TempShieldNextTurnVanillaStatus, 2),
		];

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.isPlayerTurn)
			return;
		if (state.ship.Get(Status.energyLessNextTurn) <= 0)
			return;

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = ModEntry.Instance.KokoroApi.TempShieldNextTurnVanillaStatus,
			statusAmount = 2,
			artifactPulse = Key()
		});
	}
}