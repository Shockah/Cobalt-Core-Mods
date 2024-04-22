using HarmonyLib;
using Nanoray.PluginManager;
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

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/GrimoireOfSecrets.png")).Sprite,
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
				actions.AddRange(GenerateCards(s).Select(card => new AAddCard
				{
					card = card,
					destination = CardDestination.Hand
				}));
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
					? GenerateCards(s)
					: Enumerable.Range(0, CardCount).Select(_ => (Card)new PlaceholderSecretCard()).ToList()
			});
		}
		return actions;
	}

	private List<Card> GenerateCards(State state)
	{
		List<Type> typeResults = [];

		if (typeResults.Count < CardCount)
			typeResults.Add(ModEntry.SecretAttackCardTypes[state.rngCardOfferings.NextInt() % ModEntry.SecretAttackCardTypes.Count]);

		while (typeResults.Count < CardCount)
		{
			var chosen = ModEntry.SecretNonAttackCardTypes[state.rngCardOfferings.NextInt() % ModEntry.SecretNonAttackCardTypes.Count];
			if (!typeResults.Contains(chosen))
				typeResults.Add(chosen);
		}

		var cardResults = typeResults.Select(t => (Card)Activator.CreateInstance(t)!).ToList();
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaCatArtifact) is { } artifact)
		{
			artifact.Pulse();
			var exeCardTypes = ModEntry.Instance.GetExeCardTypes().ToList();
			var exeCardType = exeCardTypes[state.rngCardOfferings.NextInt() % exeCardTypes.Count];
			var exeCard = (Card)Activator.CreateInstance(exeCardType)!;
			exeCard.discount = -1;
			cardResults.Add(exeCard);
		}

		return cardResults;
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;
}
