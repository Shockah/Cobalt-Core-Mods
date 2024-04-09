using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaPeriArtifact : Artifact, IRegisterable, IDynaHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DynaPeri", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaPeri.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaPeri", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaPeri", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, Deck.peri]);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.overdrive, Math.Max(MG.inst.g.state.ship.Get(Status.overdrive), 1))
			.Concat(StatusMeta.GetTooltips(Status.powerdrive, Math.Max(MG.inst.g.state.ship.Get(Status.powerdrive), 1)))
			.ToList();

	public void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX)
	{
		var overdrive = state.ship.Get(Status.overdrive);
		var powerdrive = state.ship.Get(Status.powerdrive);

		if (powerdrive > 0)
			combat.QueueImmediate(new AHurt
			{
				targetPlayer = false,
				hurtAmount = powerdrive,
				artifactPulse = Key()
			});

		if (overdrive > 0)
			combat.QueueImmediate(new AHurt
			{
				targetPlayer = false,
				hurtAmount = overdrive,
				hurtShieldsFirst = true,
				artifactPulse = Key()
			});
	}
}