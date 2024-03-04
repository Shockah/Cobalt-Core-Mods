using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaExeCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("DraculaExe", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DraculaExe.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DraculaExe", "name"]).Localize
		});
	}

	private int GetChoiceCount()
		=> upgrade == Upgrade.B ? 3 : 2;

	public override CardData GetData(State state)
		=> new()
		{
			artTint = ModEntry.Instance.DraculaDeck.Configuration.Definition.color.ToString(),
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "DraculaExe", "description", upgrade.ToString()], new { Count = GetChoiceCount() })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ACardOffering
			{
				amount = GetChoiceCount(),
				limitDeck = ModEntry.Instance.DraculaDeck.Deck,
				makeAllCardsTemporary = true,
				overrideUpgradeChances = false,
				canSkip = false,
				inCombat = true,
				discount = -1,
				dialogueSelector = $".summon{ModEntry.Instance.DraculaDeck.UniqueName}"
			}
		];
}
