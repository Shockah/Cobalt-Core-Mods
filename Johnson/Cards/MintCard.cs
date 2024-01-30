using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class MintCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Mint.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mint", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "FFFFFF",
			cost = 2,
			exhaust = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "Mint", "description", upgrade.ToString()], new { Damage = GetDmg(state, 2) })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AAttack
			{
				damage = GetDmg(s, 2),
				onKillActions = [
					new ADelayToRewards
					{
						Action = upgrade switch
						{
							Upgrade.A => new AUpgradeCardSelect
							{
								allowCancel = true
							},
							Upgrade.B => new ACardSelect
							{
								browseSource = CardBrowse.Source.Deck,
								browseAction = new ChooseACardToMakePermanent(),
								filterTemporary = true,
								allowCloseOverride = true
							},
							_ => new ACardSelect
							{
								browseSource = ModEntry.TemporarilyUpgradedCardsBrowseSource,
								browseAction = new MakeUpgradePermanentBrowseAction()
							},
						}
					}
				]
			}
		];

	public sealed class MakeUpgradePermanentBrowseAction : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			if (selectedCard is null)
				return;
			if (selectedCard.upgrade == Upgrade.None || !selectedCard.IsTemporarilyUpgraded())
				return;
			selectedCard.SetTemporarilyUpgraded(false);
		}
	}
}
