using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class NemesisPrepareForTroubleArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("NemesisPrepareForTrouble", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Nemesis/Artifact/PrepareForTrouble.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "PrepareForTrouble", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Nemesis", "artifact", "PrepareForTrouble", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.tempPayback, 1);

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.tempPayback, statusAmount = 1, artifactPulse = Key() });
	}
}