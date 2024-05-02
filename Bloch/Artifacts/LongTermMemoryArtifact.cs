using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class LongTermMemoryArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("LongTermMemory", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/LongTermMemory.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LongTermMemory", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "LongTermMemory", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(RetainManager.RetainStatus.Status, 1);

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.Queue(new AStatus
		{
			targetPlayer = true,
			status = RetainManager.RetainStatus.Status,
			statusAmount = 1,
			artifactPulse = Key()
		});
	}
}