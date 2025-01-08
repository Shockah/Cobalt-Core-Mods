using System;
using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Destiny;

public sealed class ExplosivoCard : Card, IRegisterable
{
	public int Counter;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DestinyDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Explosivo.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Explosivo", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var isStateful = MG.inst.g.state.route is Combat;
		var description = ModEntry.Instance.Localizations.Localize(["card", "Explosivo", "description", isStateful ? "stateful" : "stateless"], new
		{
			Period = Period,
			TimesLeft = Math.Max(Period - Counter, 1)
		});
		return upgrade switch
		{
			Upgrade.A => new() { cost = 0, description = description },
			Upgrade.B => new() { cost = 1, description = description },
			_ => new() { cost = 1, description = description },
		};
	}

	private int Period
		=> upgrade switch
		{
			Upgrade.A => 3,
			Upgrade.B => 2,
			_ => 3,
		};

	public override void OnExitCombat(State s, Combat c)
	{
		base.OnExitCombat(s, c);
		Counter = 0;
	}

	public override void OnDraw(State s, Combat c)
	{
		base.OnDraw(s, c);
		Counter++;

		if (Counter < Period)
			return;

		Counter -= Period;
		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, this, Explosive.ExplosiveTrait, true, permanent: false);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> [new ATooltipAction { Tooltips = [.. Explosive.ExplosiveTrait.Configuration.Tooltips?.Invoke(s, this) ?? []] }];
}