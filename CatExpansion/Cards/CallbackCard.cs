using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class CallbackCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/Callback.png"), StableSpr.cards_riggs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Callback", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Callback", "description", upgrade.ToString()]);
		return upgrade switch
		{
			Upgrade.A => new() { cost = 0, exhaust = true, retain = true, description = description },
			Upgrade.B => new() { cost = 0, exhaust = true, description = description },
			_ => new() { cost = 0, exhaust = true, description = description },
		};
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ACardSelect { browseSource = CardBrowse.Source.Deck, ignoreCardType = Key(), filterTemporary = true, browseAction = new BrowseAction() },
			],
			Upgrade.B => [
				new ACardSelect { browseSource = CardBrowse.Source.Deck, ignoreCardType = Key(), filterTemporary = true, browseAction = new BrowseAction { Upgrade = true } },
			],
			_ => [
				new ACardSelect { browseSource = CardBrowse.Source.Deck, ignoreCardType = Key(), filterTemporary = true, browseAction = new BrowseAction() },
			],
		};

	private sealed class BrowseAction : CardAction
	{
		public bool Upgrade;

		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["card", "Callback", "browseTitle", Upgrade ? "copyAndUpgrade" : "copy"]);

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			var baseResult = base.BeginWithRoute(g, s, c);
			if (selectedCard is null)
			{
				timer = 0;
				return baseResult;
			}
			
			var copy = selectedCard.CopyWithNewId();
			copy.drawAnim = 1;

			if (Upgrade)
				return new CardUpgrade { cardCopy = copy };
			
			c.QueueImmediate(new AAddCard
			{
				card = copy,
				destination = CardDestination.Hand,
			});
			return null;
		}
	}
}