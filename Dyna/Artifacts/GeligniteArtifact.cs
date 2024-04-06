using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class GeligniteArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Gelignite", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Gelignite.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Gelignite", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Gelignite", "description"]).Localize
		});
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new BurstCharge().GetTooltips(MG.inst.g.state ?? DB.fakeState);

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);

		var partIndexes = Enumerable.Range(0, combat.otherShip.parts.Count)
			.Where(i => combat.otherShip.parts[i].type != PType.empty)
			.ToList();
		var partIndex = partIndexes[state.rngActions.NextInt() % partIndexes.Count];
		combat.otherShip.parts[partIndex].SetStickedCharge(new BurstCharge());
	}
}