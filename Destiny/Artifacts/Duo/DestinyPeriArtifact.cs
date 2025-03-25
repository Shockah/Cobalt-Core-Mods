using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class DestinyPeriArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyPeri", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Peri.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Peri", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Peri", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, Deck.peri]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			.. StatusMeta.GetTooltips(Status.overdrive, 1),
			.. StatusMeta.GetTooltips(Status.powerdrive, 1),
		];

	public void ModifyExplosiveDamage(IDestinyApi.IHook.IModifyExplosiveDamageArgs args)
	{
		args.CurrentDamage += args.State.ship.Get(Status.overdrive);
		args.CurrentDamage += args.State.ship.Get(Status.powerdrive);
	}
}