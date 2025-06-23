using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BloodyFangArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Lacerations", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DraculaDeck.Deck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/BloodyFang.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BloodyFang", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BloodyFang", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("parttrait.weak"),
			new TTGlossary("parttrait.brittle"),
			.. StatusMeta.GetTooltips(ModEntry.Instance.BleedingStatus.Status, 1),
		];

	public override void OnEnemyGetHit(State state, Combat combat, Part? part)
	{
		base.OnEnemyGetHit(state, combat, part);
		if (part?.GetDamageModifier() is not (PDamMod.weak or PDamMod.brittle))
			return;
		
		combat.QueueImmediate(new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1, artifactPulse = Key() });
	}
}