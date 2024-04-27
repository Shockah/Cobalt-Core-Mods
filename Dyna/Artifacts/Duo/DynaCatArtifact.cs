using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaCatArtifact : Artifact, IRegisterable
{
	private static readonly Dictionary<Type, bool> BasicAttackCardCache = [];

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DynaCat", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaCat.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaCat", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaCat", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, Deck.colorless]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new BlastwaveManager.BlastwaveAction { Source = new(), Damage = 1, WorldX = 0 }.GetTooltips(DB.fakeState);

	public override void OnReceiveArtifact(State state)
	{
		base.OnReceiveArtifact(state);
		state.GetCurrentQueue().QueueImmediate(new AAddCard
		{
			destination = CardDestination.Deck,
			card = new CannonColorless(),
			amount = 2
		});
	}

	private static bool IsBasicAttackCard(State state, Combat combat, Card card)
	{
		var type = card.GetType();
		if (BasicAttackCardCache.TryGetValue(type, out var result))
			return result;

		if (card is CannonColorless)
		{
			BasicAttackCardCache[type] = true;
			return true;
		}
		if (ModEntry.Instance.MoreDifficultiesApi is { } moreDifficultiesApi && type == moreDifficultiesApi.BasicOffencesCardType)
		{
			BasicAttackCardCache[type] = true;
			return true;
		}

		foreach (var ship in StarterShip.ships.Values)
		{
			foreach (var shipCard in ship.cards)
			{
				if (shipCard.GetType() != type)
					continue;

				foreach (var action in shipCard.GetActions(state, combat))
				{
					if (action is not AAttack)
						continue;

					BasicAttackCardCache[type] = true;
					return true;
				}
			}
		}

		BasicAttackCardCache[type] = false;
		return false;
	}

	private static void Card_GetActionsOverridden_Postfix(Card __instance, State s, Combat c, ref List<CardAction> __result)
	{
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DynaCatArtifact) is not { } artifact)
			return;
		if (!IsBasicAttackCard(s, c, __instance))
			return;

		foreach (var action in __result)
		{
			if (action is AAttack attack && !attack.IsBlastwave())
			{
				attack.SetBlastwave(
					damage: ModEntry.Instance.Api.GetBlastwaveDamage(__instance, s, 1)
				);
				if (string.IsNullOrEmpty(attack.artifactPulse))
					attack.artifactPulse = artifact.Key();
				break;
			}
		}
	}
}