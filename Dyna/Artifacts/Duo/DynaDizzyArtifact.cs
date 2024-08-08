using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaDizzyArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DynaDizzy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaDizzy.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaDizzy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaDizzy", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, Deck.dizzy]);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(Status.tempShield, 1),
			.. StatusMeta.GetTooltips(Status.shield, 1),
			new TTCard
			{
				card = new FluxChargeCard
				{
					upgrade = Upgrade.B,
					temporaryOverride = true,
					exhaustOverride = true
				}
			},
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		combat.QueueImmediate(new AAddCard
		{
			card = new FluxChargeCard
			{
				upgrade = Upgrade.B,
				temporaryOverride = true,
				exhaustOverride = true
			},
			destination = CardDestination.Hand,
			artifactPulse = Key()
		});
	}
}