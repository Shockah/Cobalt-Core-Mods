using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Dyna;

internal sealed class FirecrackerArtifact : Artifact, IRegisterable, IDynaHook
{
	private const int Period = 3;

	[JsonProperty]
	private int Stacks;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Firecracker", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!),
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Firecracker.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Firecracker", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Firecracker", "description"]).Localize,
		});
	}

	public override int? GetDisplayNumber(State s)
		=> Stacks;

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(NitroManager.TempNitroStatus.Status, 1);

	public void OnChargeFired(State state, Combat combat, Ship targetShip, int worldX)
	{
		if (targetShip.isPlayerShip)
			return;

		Stacks++;
		if (Stacks < Period)
			return;

		Stacks -= Period;
		combat.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = NitroManager.TempNitroStatus.Status,
			statusAmount = 1,
			artifactPulse = Key()
		});
	}
}