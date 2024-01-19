using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BatDebitCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BatDebit", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BatmobileDeck.Deck,
				rarity = Rarity.common,
				dontOffer = true
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ship", "BatDebit", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top,
			cost = 1,
			floppable = true,
			singleUse = true,
			retain = true,
			temporary = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "ship", "BatDebit", "description", flipped ? "flipped" : "normal"])
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> flipped
			? [new ABatDebitDeposit()]
			: [new ABatDebitWithdraw()];

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
				canRunAfterKill = true,
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

			if (artifact.Charges < 5)
			{
				artifact.Charges++;
				artifact.Pulse();
			}
			
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
