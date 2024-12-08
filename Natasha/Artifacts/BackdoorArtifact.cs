using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Natasha;

internal sealed class BackdoorArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	private Status LastStatus = Status.corrode;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("Backdoor", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.NatashaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Backdoor.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Backdoor", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Backdoor", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			.. MG.inst.g.state.route is Combat ? new List<Tooltip>
			{
				new TTText(ModEntry.Instance.Localizations.Localize(["artifact", "Backdoor", "extraLine"], new { Status = LastStatus.GetLocName().ToUpper() })),
				new TTDivider(),
			} : new List<Tooltip>(),
			.. StatusMeta.GetTooltips(MG.inst.g.state.route is Combat ? LastStatus : Status.corrode, 1),
			.. ModEntry.Instance.KokoroApi.Limited.Trait.Configuration.Tooltips?.Invoke(MG.inst.g.state, null) ?? [],
		];

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		LastStatus = Status.corrode;
	}

	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.KokoroApi.Limited.Trait))
			return;
		combat.Queue(new Action { CardId = card.uuid });
	}

	private static void AStatus_Begin_Postfix(AStatus __instance, State s, Combat c)
	{
		if (__instance.targetPlayer)
			return;
		if (DB.statuses[__instance.status].isGood)
			return;
		if (s.EnumerateAllArtifacts().OfType<BackdoorArtifact>().FirstOrDefault() is not { } artifact)
			return;

		var oldAmount = c.otherShip.Get(__instance.status);
		var newAmount = __instance.mode switch
		{
			AStatusMode.Set => __instance.statusAmount,
			AStatusMode.Add => oldAmount + __instance.statusAmount,
			AStatusMode.Mult => oldAmount * __instance.statusAmount,
			_ => oldAmount
		};

		if (newAmount <= oldAmount)
			return;
		
		artifact.LastStatus = __instance.status;
	}

	private sealed class Action : CardAction
	{
		public required int CardId;
		
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.deck.Concat(c.discard).Concat(c.hand).Any(c => c.uuid == CardId))
				return;
			if (s.FindCard(CardId) is { } card && ModEntry.Instance.KokoroApi.Limited.GetLimitedUses(s, card) > 1)
				return;
			if (s.EnumerateAllArtifacts().OfType<BackdoorArtifact>().FirstOrDefault() is not { } artifact)
				return;
			
			c.QueueImmediate(new AStatus
			{
				targetPlayer = false,
				status = artifact.LastStatus,
				statusAmount = 1,
				artifactPulse = artifact.Key(),
			});
		}
	}
}