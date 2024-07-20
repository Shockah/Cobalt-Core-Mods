using Nanoray.PluginManager;
using Nickel;
using Shockah.Natasha;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class NatashaExeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("NatashaExe", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/NatashaExe.png"), StableSpr.cards_hacker).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "NatashaExe", "name"]).Localize
		});
	}

	private int GetChoiceCount()
		=> upgrade == Upgrade.B ? 3 : 2;

	public override CardData GetData(State state)
		=> new()
		{
			artTint = ModEntry.Instance.NatashaDeck.Configuration.Definition.color.ToString(),
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "NatashaExe", "description", upgrade.ToString()], new { Count = GetChoiceCount() })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ACardOffering
			{
				amount = GetChoiceCount(),
				limitDeck = ModEntry.Instance.NatashaDeck.Deck,
				makeAllCardsTemporary = true,
				overrideUpgradeChances = false,
				canSkip = false,
				inCombat = true,
				discount = -1,
				dialogueSelector = $".summon{ModEntry.Instance.NatashaDeck.UniqueName}"
			}
		];
}