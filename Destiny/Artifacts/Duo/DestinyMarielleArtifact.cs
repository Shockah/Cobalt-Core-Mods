using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Destiny;

internal sealed class DestinyMarielleArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	[JsonProperty]
	private int Counter;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		if (ModEntry.Instance.Helper.Content.Decks.LookupByUniqueName("rft.Marielle::Marielle") is not { } marielleDeck)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyMarielle", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Marielle.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Marielle", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Marielle", "description"]).Localize,
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, marielleDeck.Deck]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
	{
		var state = MG.inst.g?.state ?? DB.fakeState;
		var ship = state.route is Combat combat ? combat.otherShip : state.ship;
		return StatusMeta.GetTooltips(Status.heat, ship.heatTrigger);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		Counter = 0;
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		Counter++;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (Counter <= 0)
			return;
		
		combat.QueueImmediate(new AStatus { targetPlayer = false, status = Status.heat, statusAmount = Counter, artifactPulse = Key() });
	}

	private static void AMove_Begin_Prefix(AMove __instance, State s, Combat c, out int __state)
	{
		var ship = __instance.targetPlayer ? s.ship : c.otherShip;
		__state = ship.x;
	}

	private static void AMove_Begin_Postfix(AMove __instance, State s, Combat c, in int __state)
	{
		if (!__instance.targetPlayer)
			return;
		
		var ship = __instance.targetPlayer ? s.ship : c.otherShip;
		if (ship.x == __state)
			return;
		if (s.EnumerateAllArtifacts().OfType<DestinyMarielleArtifact>().FirstOrDefault() is not { } artifact)
			return;

		artifact.Counter = 0;
	}
}