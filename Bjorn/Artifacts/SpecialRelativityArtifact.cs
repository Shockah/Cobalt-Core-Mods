using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Bjorn;

internal sealed class SpecialRelativityArtifact : Artifact, IRegisterable, IBjornApi.IHook
{
	internal static IArtifactEntry Entry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Entry = helper.Content.Artifacts.RegisterArtifact("SpecialRelativity", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BjornDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/SpecialRelativity.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SpecialRelativity", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "SpecialRelativity", "description"]).Localize
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(RelativityManager.RelativityStatus.Status, 1);

	public void ModifyRelativityLimit(IBjornApi.IHook.IModifyRelativityLimitArgs args)
		=> args.Limit++;

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