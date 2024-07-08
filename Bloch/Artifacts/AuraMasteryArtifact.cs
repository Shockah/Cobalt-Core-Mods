using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class AuraMasteryArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	private int Counter;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("AuraMastery", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/AuraMastery.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "AuraMastery", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "AuraMastery", "description"]).Localize
		});
	}

	public override int? GetDisplayNumber(State s)
		=> Counter;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			..StatusMeta.GetTooltips(AuraManager.VeilingStatus.Status, 2),
			..StatusMeta.GetTooltips(AuraManager.FeedbackStatus.Status, 1),
			..StatusMeta.GetTooltips(AuraManager.InsightStatus.Status, 1),
		];

	public override void AfterPlayerStatusAction(State state, Combat combat, Status status, AStatusMode mode, int statusAmount)
	{
		base.AfterPlayerStatusAction(state, combat, status, mode, statusAmount);
		if (status != AuraManager.VeilingStatus.Status || mode != AStatusMode.Add || statusAmount <= 0)
			return;

		Counter++;
		while (Counter >= 2)
		{
			Counter -= 2;
			combat.QueueImmediate(new AStatus
			{
				targetPlayer = true,
				status = state.rngActions.NextInt() % 2 == 0 ? AuraManager.FeedbackStatus.Status : AuraManager.InsightStatus.Status,
				statusAmount = 1,
				artifactPulse = Key()
			});
		}
	}
}