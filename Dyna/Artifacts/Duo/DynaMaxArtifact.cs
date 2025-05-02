using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaMaxArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	private int Counter;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DynaMax", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaMax.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaMax", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaMax", "description"]).Localize,
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, Deck.hacker]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToExhaust)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToExhaust_Postfix))
		);
	}

	public override int? GetDisplayNumber(State s)
		=> Counter;

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("cardtrait.exhaust"),
			..new BlastwaveManager.BlastwaveAction { Source = new(), Damage = null, IsStunwave = true, LocalX = 0 }.GetTooltips(DB.fakeState)
		];

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.isPlayerTurn)
			return;
		if (state.ship.Get(Status.shard) < 3)
			return;

		combat.QueueImmediate([
			new AStatus { targetPlayer = true, status = Status.shard, statusAmount = -3, artifactPulse = Key() },
			new AStatus { targetPlayer = true, status = NitroManager.NitroStatus.Status, statusAmount = 1, artifactPulse = Key() },
		]);
	}

	private static void Combat_SendCardToExhaust_Postfix(Combat __instance, State s)
	{
		if (s.EnumerateAllArtifacts().OfType<DynaMaxArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (++artifact.Counter < 5)
			return;

		artifact.Counter -= 5;
		__instance.QueueImmediate(new AAttack
		{
			damage = Card.GetActualDamage(s, 0),
			artifactPulse = artifact.Key(),
		}.SetBlastwave(damage: 0, isStunwave: true));
	}
}