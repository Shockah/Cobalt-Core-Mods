using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class WaterfallModelCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/WaterfallModel.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "WaterfallModel", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 3, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "WaterfallModel", "description"], new { Progress = GadgetProgressAmount, Lock = EngineLockAmount }) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus { targetPlayer = true, status = GadgetManager.GetCorrectStatus(s), statusAmount = GadgetProgressAmount },
			new AStatus { targetPlayer = true, status = Status.lockdown, statusAmount = EngineLockAmount },
			new OnAnalyzeAction { Action = ModEntry.Instance.KokoroApi.PlayCardsFromAnywhere.MakeAction(this).AsCardAction },
		];

	private int GadgetProgressAmount
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		get => upgrade.Switch(3, 4, 5);
	}

	private int EngineLockAmount
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		get => upgrade.Switch(2, 2, 3);
	}
}
