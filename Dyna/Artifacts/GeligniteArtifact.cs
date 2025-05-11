using System;
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
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Gelignite", "description"]).Localize,
		});
	}

	public override List<Tooltip> GetExtraTooltips()
		=> new BurstCharge().GetTooltips(MG.inst.g?.state ?? DB.fakeState);

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);

		var picks = Enumerable.Range(0, combat.otherShip.parts.Count)
			.Where(i => combat.otherShip.parts[i].type != PType.empty)
			.Shuffle(state.rngActions)
			.Select(i =>
			{
				var neighbors = 0;
				if (combat.otherShip.GetPartAtLocalX(i - 1) is { } left && left.type != PType.empty)
					neighbors++;
				if (combat.otherShip.GetPartAtLocalX(i + 1) is { } right && right.type != PType.empty)
					neighbors++;
				return (Index: i, Neighbors: neighbors);
			})
			.OrderByDescending(e => e.Neighbors)
			.ToList();

		var zippedPicks = GetAllCombinations()
			.OrderByDescending(e => e.Distance >= 2)
			.ThenByDescending(e => e.First.Neighbors + e.Second.Neighbors);

		if (zippedPicks.FirstOrNull() is not { } pickPair)
			return;
		
		combat.otherShip.parts[pickPair.First.Index].SetStickedCharge(new BurstCharge());
		combat.otherShip.parts[pickPair.Second.Index].SetStickedCharge(new BurstCharge());
		Pulse();

		IEnumerable<((int Index, int Neighbors) First, (int Index, int Neighbors) Second, int Distance)> GetAllCombinations()
		{
			foreach (var first in picks)
				foreach (var second in picks)
					yield return (first, second, Math.Abs(first.Index - second.Index));
		}
	}
}