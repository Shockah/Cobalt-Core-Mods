using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shockah.Johnson;

internal sealed class OutsourceCard : Card, IRegisterable
{
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Outsource.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Outsource", "name"]).Localize
		});
	}

	private int CardCount
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		get => upgrade switch
		{
			Upgrade.A => 7,
			Upgrade.B => 3,
			_ => 5
		};
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			art = StableSpr.cards_SecondOpinions,
			cost = 0,
			exhaust = upgrade != Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "Outsource", "description"], new { Count = CardCount })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new ACardOffering
			{
				amount = CardCount,
				makeAllCardsTemporary = true,
				overrideUpgradeChances = false,
				canSkip = false,
				inCombat = true
			}
		];
}
