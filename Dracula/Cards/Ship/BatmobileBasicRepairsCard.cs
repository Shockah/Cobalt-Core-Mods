using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BatmobileBasicRepairsCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Batmobile.BasicRepairs", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ship", "BasicRepairs", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.None ? 3 : 2,
			exhaust = true,
			description = upgrade == Upgrade.B ? ModEntry.Instance.Localizations.Localize(["card", "ship", "BasicRepairs", "descriptionB"]) : null
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
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

			artifact.Charges += Charges;
			artifact.Pulse();
		}
	}

	public sealed class ABatDebitWithdraw : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var artifact = s.EnumerateAllArtifacts().OfType<BloodBankArtifact>().FirstOrDefault();
			if (artifact is null || artifact.Charges <= 0)
			{
				timer = 0;
				return;
			}

			artifact.Charges -= 2; // healing grants 1
			artifact.Pulse();
			c.QueueImmediate(new AHeal
			{
				targetPlayer = true,
				healAmount = 1,
				artifactPulse = artifact.Key()
			});
		}
	}

	public sealed class ABatDebitDeposit : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			var artifact = s.EnumerateAllArtifacts().OfType<BloodBankArtifact>().FirstOrDefault();
			if (artifact is null)
			{
				timer = 0;
				return;
			}

			artifact.Charges++;
			artifact.Pulse();
			if (s.ship.Get(Status.perfectShield) > 0)
				c.QueueImmediate(new AStatus
				{
					targetPlayer = true,
					status = Status.perfectShield,
					statusAmount = -1,
					artifactPulse = artifact.Key()
				});
			else
				c.QueueImmediate(new AHurt
				{
					targetPlayer = true,
					hurtAmount = 1,
					artifactPulse = artifact.Key()
				});
		}
	}
}
