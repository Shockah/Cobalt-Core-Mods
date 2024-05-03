using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shockah.Dracula;

internal sealed class GrimoireOfSecretsCard : Card, IDraculaCard
{
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
		=> [
			new Action
			{
				CardCount = CardCount,
				Mode = upgrade == Upgrade.B ? Action.ModeEnum.PutInHand : Action.ModeEnum.Choose
			},
			new ATooltipAction
			{
				Tooltips = [new TTCard { card = new PlaceholderSecretCard() }]
			}
		];

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

	private sealed class Action : CardAction
	{
		public enum ModeEnum
		{
			Choose,
			PutInHand
		}

		public required int CardCount;
		public required ModeEnum Mode;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			List<Type> typeResults = [];

			if (typeResults.Count < CardCount)
				typeResults.Add(ModEntry.SecretAttackCardTypes[s.rngCardOfferings.NextInt() % ModEntry.SecretAttackCardTypes.Count]);

			while (typeResults.Count < CardCount)
			{
				var chosen = ModEntry.SecretNonAttackCardTypes[s.rngCardOfferings.NextInt() % ModEntry.SecretNonAttackCardTypes.Count];
				if (!typeResults.Contains(chosen))
					typeResults.Add(chosen);
			}

			var cardResults = typeResults.Select(t => (Card)Activator.CreateInstance(t)!).ToList();
			if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaCatArtifact) is { } artifact)
			{
				artifact.Pulse();
				var exeCardTypes = ModEntry.Instance.GetExeCardTypes().ToList();
				var exeCardType = exeCardTypes[s.rngCardOfferings.NextInt() % exeCardTypes.Count];
				var exeCard = (Card)Activator.CreateInstance(exeCardType)!;
				exeCard.discount = -1;
				cardResults.Add(exeCard);
			}

			switch (Mode)
			{
				case ModeEnum.Choose:
					c.QueueImmediate(new ASpecificCardOffering { Cards = cardResults });
					break;
				case ModeEnum.PutInHand:
					c.QueueImmediate(cardResults.Select(card => new AAddCard { card = card, destination = CardDestination.Hand }));
					break;
			}
		}
	}
}
