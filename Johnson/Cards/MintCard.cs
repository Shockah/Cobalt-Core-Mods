﻿using Nanoray.PluginManager;
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
							Upgrade.A => new ACardSelect
							{
								browseSource = ModEntry.NonPermanentlyUpgradedCardsBrowseSource,
								browseAction = new UpgradeNonPermanentlyUpgradedCardBrowseAction(),
								allowCancel = true
							},
							Upgrade.B => new ACardSelect
							{
								browseSource = CardBrowse.Source.Deck,
								browseAction = new ChooseACardToMakePermanent(),
								filterTemporary = true,
								allowCloseOverride = true,
								allowCancel = true
							},
							_ => new ACardSelect
							{
								browseSource = ModEntry.TemporarilyUpgradedCardsBrowseSource,
								browseAction = new MakeUpgradePermanentBrowseAction(),
								allowCancel = true
							},
						}
					}
				]
			},
			new ATooltipAction
			{
				Tooltips = upgrade switch
				{
					Upgrade.A => new UpgradeNonPermanentlyUpgradedCardBrowseAction().GetTooltips(s),
					Upgrade.B => [new TTGlossary("cardtrait.temporary")],
					_ => new MakeUpgradePermanentBrowseAction().GetTooltips(s),
				}
			}
		];

	public sealed class UpgradeNonPermanentlyUpgradedCardBrowseAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("action.upgradeCard")];

		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			timer = 0;
			var baseResult = base.BeginWithRoute(g, s, c);
			if (selectedCard is null)
				return baseResult;

			ModEntry.Instance.KokoroApi.TemporaryUpgrades.SetTemporaryUpgrade(selectedCard, null);
			return new CardUpgrade
			{
				cardCopy = Mutil.DeepCopy(selectedCard)
			};
		}
	}

	public sealed class MakeUpgradePermanentBrowseAction : CardAction
	{
		public override List<Tooltip> GetTooltips(State s)
			=> [
				new TTGlossary("action.upgradeCard"),
				ModEntry.Instance.KokoroApi.TemporaryUpgrades.UpgradeTooltip,
			];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			if (selectedCard is null)
				return;
			if (ModEntry.Instance.KokoroApi.TemporaryUpgrades.GetTemporaryUpgrade(selectedCard) is not { } temporaryUpgrade)
				return;
			ModEntry.Instance.KokoroApi.TemporaryUpgrades.SetTemporaryUpgrade(selectedCard, null);
			ModEntry.Instance.KokoroApi.TemporaryUpgrades.SetPermanentUpgrade(selectedCard, temporaryUpgrade);
		}
	}
}
