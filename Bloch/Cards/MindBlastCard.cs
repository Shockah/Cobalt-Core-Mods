using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MindBlastCard : Card, IRegisterable
{
	public int PlayCounter = 0;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Prism,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/MindBlast.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MindBlast", "name"]).Localize
		});
	}

	private int GetDamage(State state)
		=> upgrade switch
		{
			Upgrade.B => GetDmg(state, 1 + PlayCounter),
			_ => GetDmg(state, 2 + PlayCounter * 2)
		};

	public override CardData GetData(State state)
		=> new()
		{
			cost = 2,
			recycle = upgrade == Upgrade.A,
			retain = upgrade == Upgrade.A,
			exhaust = upgrade == Upgrade.B,
			description = ModEntry.Instance.Localizations.Localize(["card", "MindBlast", "description", upgrade.ToString()], new { Damage = GetDamage(state) })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AAttack
				{
					damage = GetDamage(s)
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new CountUpAction { CardId = uuid }
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new ExhaustCardAction { CardId = uuid }
				}
			],
			_ => [
				new AAttack
				{
					damage = GetDamage(s)
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new CountUpAction { CardId = uuid }
				}
			]
		};

	public override void OnExitCombat(State s, Combat c)
	{
		base.OnExitCombat(s, c);

		if (upgrade != Upgrade.B)
			PlayCounter = 0;
	}

	private sealed class CountUpAction : CardAction
	{
		public required int CardId;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (s.FindCard(CardId) is not MindBlastCard card)
				return;
			card.PlayCounter++;
		}
	}
}
