using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class JohnsonDizzyArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("JohnsonDizzy", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/JohnsonDizzy.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonDizzy", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "JohnsonDizzy", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.JohnsonDeck.Deck, Deck.dizzy]);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			ModEntry.Instance.Api.GetStrengthenTooltip(1),
			..StatusMeta.GetTooltips(Status.shield, 1)
		];

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (!combat.isPlayerTurn || combat.turn != 1)
			return;

		combat.Queue([
			new ADelay(),
			new ACardSelect
			{
				browseAction = new StrengthenBrowseAction { Amount = 1 },
				browseSource = CardBrowse.Source.Deck,
			}
		]);
	}

	private static void Card_GetActionsOverridden_Postfix(Card __instance, State s, ref List<CardAction> __result)
	{
		var strenghten = __instance.GetStrengthen();
		if (strenghten == 0)
			return;

		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is JohnsonDizzyArtifact) is not { } artifact)
			return;

		foreach (var baseAction in __result)
		{
			foreach (var wrappedAction in ModEntry.Instance.KokoroApi.WrappedActions.GetWrappedCardActionsRecursively(baseAction))
			{
				if (wrappedAction is AStatus { mode: AStatusMode.Add, status: Status.shield } statusAction)
				{
					statusAction.statusAmount += strenghten;
					if (string.IsNullOrEmpty(statusAction.artifactPulse))
						statusAction.artifactPulse = artifact.Key();
				}
			}
		}
	}
}