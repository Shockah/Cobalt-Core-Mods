using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class DestinyDizzyArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyDizzy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Dizzy.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Dizzy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Dizzy", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, Deck.dizzy]);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(DB.fakeState, null) ?? [],
			new TTGlossary("action.stun")
		];

	public void OnExplosiveTrigger(IDestinyApi.IHook.IOnExplosiveTriggerArgs args)
	{
		args.AttackAction.stunEnemy = true;
		if (string.IsNullOrEmpty(args.AttackAction.artifactPulse))
			args.AttackAction.artifactPulse = Key();
		else
			Pulse();
	}
}