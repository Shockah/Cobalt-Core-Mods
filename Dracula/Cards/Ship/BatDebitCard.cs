using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class BatDebitCard : Card, IDraculaCard
{
	private static ISpriteEntry TopArt = null!;
	private static ISpriteEntry BottomArt = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		TopArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Ship/BatDebitTop.png"));
		BottomArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Ship/BatDebitBottom.png"));

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
			art = (flipped ? BottomArt : TopArt).Sprite,
			artTint = "FFFFFF",
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

			artifact.Charges -= 1;
			artifact.Pulse();

			var action = new AHeal
			{
				targetPlayer = true,
				healAmount = 1,
				canRunAfterKill = true,
				artifactPulse = artifact.Key()
			};
			ModEntry.Instance.Helper.ModData.SetModData(action, "FromBloodBank", true);

			c.QueueImmediate(action);
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
