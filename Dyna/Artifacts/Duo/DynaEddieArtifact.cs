using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaEddieArtifact : Artifact, IRegisterable
{
	private static ISpriteEntry ActiveSprite = null!;
	private static ISpriteEntry InactiveSprite = null!;

	[JsonProperty]
	public bool TriggeredThisCombat { get; set; } = false;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		// TODO: use Eddie's API

		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (!helper.ModRegistry.LoadedMods.ContainsKey("TheJazMaster.Eddie"))
			return;

		ActiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaEddie.png"));
		InactiveSprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaEddieInactive.png"));

		helper.Content.Artifacts.RegisterArtifact("DynaEddie", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = ActiveSprite.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaEddie", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaEddie", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, helper.Content.Decks.LookupByUniqueName("TheJazMaster.Eddie::Eddie.EddieDeck")!.Deck]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Prefix))
		);
	}

	public override Spr GetSprite()
		=> (TriggeredThisCombat ? InactiveSprite : ActiveSprite).Sprite;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			..StatusMeta.GetTooltips(Status.energyLessNextTurn, Math.Max(MG.inst.g.state.ship.Get(Status.energyLessNextTurn), 1)),
			..StatusMeta.GetTooltips(Status.energyNextTurn, 1)
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	private static void AStatus_Begin_Prefix(AStatus __instance, State s, Combat c)
	{
		if (__instance.status != Status.energyLessNextTurn || !__instance.targetPlayer)
			return;
		if (s.EnumerateAllArtifacts().OfType<DynaEddieArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (artifact.TriggeredThisCombat)
			return;

		var currentAmount = s.ship.Get(Status.energyLessNextTurn);
		var newAmount = __instance.mode switch
		{
			AStatusMode.Set => __instance.statusAmount,
			AStatusMode.Add => currentAmount + __instance.statusAmount,
			AStatusMode.Mult => currentAmount * __instance.statusAmount,
			_ => currentAmount
		};
		if (newAmount <= currentAmount)
			return;

		artifact.Pulse();
		artifact.TriggeredThisCombat = true;
		__instance.mode = AStatusMode.Add;
		__instance.status = Status.energyNextTurn;
		__instance.statusAmount = 1;
	}
}