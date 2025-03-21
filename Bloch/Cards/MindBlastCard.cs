using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Shockah.Bloch;

internal sealed class MindBlastCard : Card, IRegisterable
{
	[JsonProperty]
	private int PlayCounter;

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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/MindBlast.png"), StableSpr.cards_Prism).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MindBlast", "name"]).Localize
		});
	}

	private int GetDamage(State state)
		=> GetDmg(state, 2 + PlayCounter * 2);

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 1 : 2,
			retain = true,
			description = ModEntry.Instance.Localizations.Localize(["card", "MindBlast", "description", upgrade.ToString()], new { Damage = GetDamage(state), Gain = 2 })
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new CountUpAction { CardId = uuid },
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new AAttack { damage = GetDamage(s) }).AsCardAction,
			],
			_ => [
				new AAttack { damage = GetDamage(s) },
				ModEntry.Instance.KokoroApi.OnDiscard.MakeAction(new CountUpAction { CardId = uuid }).AsCardAction,
			]
		};

	public override void OnExitCombat(State s, Combat c)
	{
		base.OnExitCombat(s, c);
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
