using FSPRO;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class SafetyProtocolCard : Card, IRegisterable
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
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SafetyProtocol.png"), StableSpr.cards_Shield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SafetyProtocol", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "SafetyProtocol", "description", upgrade.ToString(), flipped ? "energy" : "analyze"]);
		return upgrade.Switch<CardData>(
			none: () => new() { cost = 0, floppable = true, infinite = true, description = description },
			a: () => new() { cost = 0, floppable = true, infinite = true, description = description },
			b: () => new() { cost = 0, floppable = true, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [new ActionCost { Analyze = !flipped, EnergyCost = flipped ? 1 : 0, Actions = [
				new SmartShieldAction { Amount = 1 },
			] }],
			a: () => [new ActionCost { Analyze = !flipped, EnergyCost = flipped ? 1 : 0, Actions = [
				new SmartShieldAction { Amount = 1 },
				new ADrawCard { count = 1 },
			] }],
			b: () => [new ActionCost { Analyze = !flipped, EnergyCost = flipped ? 1 : 0, Actions = [
				new SmartShieldAction { Amount = 2 },
			] }]
		);

	private sealed class ActionCost : CardAction
	{
		public int EnergyCost;
		public bool Analyze;
		public required List<CardAction> Actions;

		public override List<Tooltip> GetTooltips(State s)
			=> Analyze
				? new AnalyzeCostAction { Actions = Actions }.GetTooltips(s)
				: Actions.SelectMany(a => a.GetTooltips(s)).ToList();

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			if (Analyze)
			{
				timer = 0;
				c.QueueImmediate(new AnalyzeCostAction { Actions = Actions });
			}
			else if (EnergyCost > 0)
			{
				if (c.energy < EnergyCost)
				{
					timer = 0;
					Audio.Play(Event.ZeroEnergy);
					return;
				}
				else
				{
					c.energy -= EnergyCost;
					c.QueueImmediate(Actions);
				}
			}
			else
			{
				timer = 0;
				c.QueueImmediate(Actions);
			}
		}
	}
}