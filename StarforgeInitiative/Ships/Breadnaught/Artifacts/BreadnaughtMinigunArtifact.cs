using System;
using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtMinigunArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	private int LastEnergy;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BreadnaughtMinigun", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Breadnaught/Artifact/Minigun.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "Minigun", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "Minigun", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(BarrelSpinManager.BarrelSpinStatus.Status, 1);

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		LastEnergy = combat.energy;
	}

	public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
	{
		base.OnQueueEmptyDuringPlayerTurn(state, combat);
		
		var energySpent = Math.Max(LastEnergy - combat.energy, 0);
		LastEnergy = combat.energy;
		if (energySpent <= 0)
			return;
		
		combat.QueueImmediate(new AStatus { targetPlayer = true, status = BarrelSpinManager.BarrelSpinStatus.Status, statusAmount = energySpent, artifactPulse = Key(), timer = 0.1 });
	}
}