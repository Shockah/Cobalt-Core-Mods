using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class MergerCard : Card, IRegisterable
{
	private static bool IsDuringTryPlayCard = false;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.JohnsonDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Merger.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Merger", "name"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [
			new AVariableHint
			{
				status = Status.shield,
			}
		];

		if (upgrade == Upgrade.A)
		{
			actions.AddRange([
				new AStrengthen
				{
					CardId = uuid,
					Amount = s.ship.Get(Status.shield),
					xHint = 1
				},
				new AAttack
				{
					damage = GetDmg(s, 1 + (IsDuringTryPlayCard ? s.ship.Get(Status.shield) : 0))
				}
			]);
		}
		else
		{
			actions.AddRange([
				new AAttack
				{
					damage = GetDmg(s, 1)
				},
				new AStrengthen
				{
					CardId = uuid,
					Amount = s.ship.Get(Status.shield),
					xHint = 1
				}
			]);
		}

		actions.Add(new AStatus
		{
			targetPlayer = true,
			mode = AStatusMode.Set,
			status = Status.shield,
			statusAmount = upgrade == Upgrade.B ? 1 : 0
		});
		return actions;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}
