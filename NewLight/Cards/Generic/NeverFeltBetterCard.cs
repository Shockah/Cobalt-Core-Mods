using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.NewLight;

internal class NeverFeltBetterCard : GuardianCard, IRegisterable
{
	private bool DuringSafelyGetDataWithOverrides;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GuardianDeck.Deck,
				rarity = RARITY,
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Ability.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Card", "Generic", "NeverFeltBetter", "Name"]).Localize,
		});
		
		Abilities.SetBaseCooldown(entry.UniqueName, Upgrade.B, 1);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => base.GetData(state) with { cost = 0, exhaust = true },
			_ => base.GetData(state) with { cost = 1, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			ModEntry.Instance.KokoroApi.EnergyAsStatus.MakeVariableHint().AsCardAction,
			new AHeal { targetPlayer = true, healAmount = c.energy - SafelyGetDataWithOverrides(s).cost, xHint = 1 },
			new AEndTurn(),
		];

	private CardData SafelyGetDataWithOverrides(State state)
	{
		if (DuringSafelyGetDataWithOverrides)
			return new();
		
		try
		{
			DuringSafelyGetDataWithOverrides = true;
			return GetDataWithOverrides(state);
		}
		finally
		{
			DuringSafelyGetDataWithOverrides = false;
		}
	}
}