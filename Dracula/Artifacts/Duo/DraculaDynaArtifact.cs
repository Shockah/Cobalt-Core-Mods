using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaDynaArtifact : Artifact, IRegisterable, IDynaHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.DynaApi is not { } dynaApi)
			return;

		helper.Content.Artifacts.RegisterArtifact("DraculaDyna", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaDyna.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDyna", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDyna", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DraculaDeck.Deck, dynaApi.DynaDeck.Deck]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(ModEntry.Instance.BleedingStatus.Status, 1);

	public void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX)
	{
		combat.QueueImmediate(new AStatus
		{
			targetPlayer = ship.isPlayerShip,
			status = ModEntry.Instance.BleedingStatus.Status,
			statusAmount = 1,
			artifactPulse = Key()
		});
	}
}