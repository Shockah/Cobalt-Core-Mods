﻿using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class JohnsonExeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("JohnsonExe", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/JohnsonExe.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "JohnsonExe", "name"]).Localize
		});
	}

	private int GetChoiceCount()
		=> upgrade == Upgrade.B ? 3 : 2;

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "ffffff",
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "JohnsonExe", "description", upgrade.ToString()], new { Count = GetChoiceCount() })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ACardOffering
			{
				amount = GetChoiceCount(),
				limitDeck = ModEntry.Instance.JohnsonDeck.Deck,
				makeAllCardsTemporary = true,
				overrideUpgradeChances = false,
				canSkip = false,
				inCombat = true,
				discount = -1,
				dialogueSelector = $".summon{ModEntry.Instance.JohnsonDeck.UniqueName}"
			}
		];
}