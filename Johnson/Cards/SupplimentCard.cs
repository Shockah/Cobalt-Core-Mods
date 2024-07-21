using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class SupplimentCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Suppliment.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Suppliment", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 1,
			description = ModEntry.Instance.Localizations.Localize(["card", "Suppliment", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus
			{
				targetPlayer = true,
				status = Status.shield,
				statusAmount = 1,
			},
			new PositionalAction
			{
				Leftmost = upgrade == Upgrade.A,
				Rightmost = true,
				Discount = true,
				Strengthen = upgrade == Upgrade.B,
			}
		];

	private sealed class PositionalAction : CardAction
	{
		public bool Leftmost;
		public bool Rightmost;
		public bool Discount = true;
		public bool Strengthen = false;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);

			if (Leftmost && c.hand.FirstOrDefault() is { } leftmostCard)
			{
				if (Discount)
					leftmostCard.discount--;
				if (Strengthen)
					leftmostCard.AddStrengthen(1);
			}
			if (Rightmost && c.hand.LastOrDefault() is { } rightmostCard)
			{
				if (Discount)
					rightmostCard.discount--;
				if (Strengthen)
					rightmostCard.AddStrengthen(1);
			}
		}

		public override List<Tooltip> GetTooltips(State s)
		{
			var tooltips = new List<Tooltip>();
			if (Discount)
				tooltips.Add(new TTGlossary("cardtrait.discount", 1));
			if (Strengthen)
				tooltips.Add(ModEntry.Instance.Api.GetStrengthenTooltip(1));
			return tooltips;
		}
	}
}
