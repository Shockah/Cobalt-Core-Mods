using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaBooksArtifact : Artifact, IRegisterable
{
	internal const int HealAmount = 2;

	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	private bool TriggeredThisCombat;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		ActiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaBooks.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaBooksInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("DraculaBooks", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaBooks", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaBooks", "description"], new { HealAmount }).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DraculaDeck.Deck, Deck.shard]);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Finalizer))
		);
	}

	public override Spr GetSprite()
		=> (TriggeredThisCombat ? InactiveSprite : ActiveSprite).Sprite;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			..StatusMeta.GetTooltips(Status.shard, (MG.inst.g.state?.ship ?? DB.fakeState.ship).GetMaxShard()),
			..new AHeal { targetPlayer = true, healAmount = HealAmount }.GetTooltips(MG.inst.g.state ?? DB.fakeState),
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	private static void Combat_DrainCardActions_Prefix(G g, ref int __state)
		=> __state = g.state.ship.Get(Status.shard);

	private static void Combat_DrainCardActions_Finalizer(Combat __instance, G g, ref int __state)
	{
		if (__state <= 0)
			return;
		if (g.state.ship.Get(Status.shard) > 0)
			return;
		if (g.state.EnumerateAllArtifacts().OfType<DraculaBooksArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisCombat)
			return;

		artifact.TriggeredThisCombat = true;
		__instance.QueueImmediate(new AHeal
		{
			targetPlayer = true,
			healAmount = HealAmount,
			artifactPulse = artifact.Key()
		});
	}
}