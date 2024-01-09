using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shockah.Dracula;

internal sealed class GrimoireOfSecretsCard : Card, IDraculaCard
{
	private static bool IsDuringTryPlayCard { get; set; } = false;

	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("GrimoireOfSecrets", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "GrimoireOfSecrets", "name"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
	}

	
	private int CardCount
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		get => upgrade == Upgrade.A ? 4 : 3;
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 2 : 1,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "GrimoireOfSecrets", "description", upgrade.ToString()], new { Count = CardCount })
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> actions = [];
		if (upgrade == Upgrade.B)
		{
			if (IsDuringTryPlayCard)
			{
				foreach (var cardType in GenerateCards(s.rngActions))
					actions.Add(new AAddCard
					{
						card = (Card)Activator.CreateInstance(cardType)!,
						destination = CardDestination.Hand
					});
			}
			else
			{
				actions.Add(new AAddCard
				{
					card = new PlaceholderSecretCard(),
					amount = CardCount
				});
			}
		}
		else
		{
			actions.Add(new ASpecificCardOffering
			{
				Cards = IsDuringTryPlayCard
					? GenerateCards(s.rngActions).Select(t => (Card)Activator.CreateInstance(t)!).ToList()
					: Enumerable.Range(0, CardCount).Select(_ => (Card)new PlaceholderSecretCard()).ToList()
			});
		}
		return actions;
	}

	private List<Type> GenerateCards(Rand rng)
	{
		List<Type> results = [];
		if (results.Count < CardCount)
			results.Add(ModEntry.SecretAttackCardTypes[rng.NextInt() % ModEntry.SecretAttackCardTypes.Count]);
		while (results.Count < CardCount)
		{
			var chosen = ModEntry.SecretNonAttackCardTypes[rng.NextInt() % ModEntry.SecretNonAttackCardTypes.Count];
			if (!results.Contains(chosen))
				results.Add(chosen);
		}
		return results;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}
