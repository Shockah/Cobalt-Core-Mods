using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BatmobileBasicRepairsCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Batmobile.BasicRepairs", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true
			},
			Art = StableSpr.cards_Repairs,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ship", "BasicRepairs", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "77ff85",
			cost = 2,
			exhaust = true,
			description = upgrade == Upgrade.B ? ModEntry.Instance.Localizations.Localize(["card", "ship", "BasicRepairs", "descriptionB"]) : null
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AHeal
				{
					targetPlayer = true,
					healAmount = 1
				}
			],
			Upgrade.B => [
				new ABatDebitCharge
				{
					Charges = 3
				}
			],
			_ => [
				new AHeal
				{
					targetPlayer = true,
					healAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyLessNextTurn,
					statusAmount = 1
				}
			]
		};

	public sealed class ABatDebitCharge : CardAction
	{
		[JsonProperty]
		public int Charges;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var artifact = s.EnumerateAllArtifacts().OfType<BloodBankArtifact>().FirstOrDefault();
			if (artifact is null)
			{
				timer = 0;
				return;
			}

			if (artifact.Charges >= 5)
				return;

			artifact.Charges = Math.Min(artifact.Charges + 3, 5);
			artifact.Pulse();
		}
	}
}
