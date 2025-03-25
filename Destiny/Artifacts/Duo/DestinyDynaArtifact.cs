using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Dyna;

namespace Shockah.Destiny;

internal sealed class DestinyDynaArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	private static IDynaApi DynaApi = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.Helper.ModRegistry.GetApi<IDynaApi>("Shockah.Dyna") is not { } dynaApi)
			return;

		DynaApi = dynaApi;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyDyna", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Dyna.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Dyna", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Dyna", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, dynaApi.DynaDeck.Deck]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			.. DynaApi.SetBlastwave(new AAttack { damage = 1 }, 1).GetTooltips(DB.fakeState).Where(t => t is not TTGlossary { key: "action.attack.name" })
		];

	public void OnExplosiveTrigger(IDestinyApi.IHook.IOnExplosiveTriggerArgs args)
	{
		if (args.Action is not AAttack attack)
			return;

		attack = DynaApi.SetBlastwave(attack, 1);
		args.Action = attack;
		if (string.IsNullOrEmpty(attack.artifactPulse))
			attack.artifactPulse = Key();
		else
			Pulse();
	}
}