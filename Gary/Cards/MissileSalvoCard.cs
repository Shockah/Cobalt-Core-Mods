using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using daisyowl.text;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class MissileSalvoCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/MissileSalvo.png"), StableSpr.cards_goat).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "MissileSalvo", "name"]).Localize,
		});

		var hook = new Hook();
		ModEntry.Instance.KokoroApi.CardRendering.RegisterHook(hook);
	}

	public override CardData GetData(State state)
		=> new() { cost = 3, exhaust = true, description = ModEntry.Instance.Localizations.Localize(["card", "MissileSalvo", "description", upgrade.ToString()]) };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [new Action { Thing = new Missile { missileType = MissileType.seeker } }],
			Upgrade.A => [new Action { Thing = new Missile { missileType = MissileType.heavy } }],
			_ => [new Action { Thing = new Missile { missileType = MissileType.normal } }],
		};

	private sealed class Action : CardAction
	{
		public bool FromPlayer = true;
		public required StuffBase Thing;

		public override List<Tooltip> GetTooltips(State s)
		{
			var ship = FromPlayer ? s.ship : (s.route as Combat ?? DB.fakeCombat).otherShip;
			return
			[
				.. StatusMeta.GetTooltips(Cram.CramStatus.Status, Math.Max(ship.Get(Cram.CramStatus.Status), 1)),
				.. Thing.GetTooltips(),
			];
		}

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			var ship = FromPlayer ? s.ship : c.otherShip;
			var rng = FromPlayer ? s.rngActions : s.rngAi;
			var amount = ship.Get(Cram.CramStatus.Status);

			if (amount <= 0)
				return;

			var totalTime = TIMER_DEFAULT;
			for (var i = 1; i < amount; i++)
				totalTime += Math.Max(TIMER_DEFAULT * (20 - i) / 40, 0.04);

			var thing = Mutil.DeepCopy(Thing);
			thing.targetPlayer = !FromPlayer;

			c.QueueImmediate([
				new AStatus { targetPlayer = FromPlayer, mode = AStatusMode.Set, status = Cram.CramStatus.Status, statusAmount = 0 },
				.. Enumerable.Range(0, amount).Select(_ => new ASpawn { fromPlayer = FromPlayer, thing = Mutil.DeepCopy(thing), timer = totalTime / amount, fromX = rng.NextInt() % ship.parts.Count }.SetCrammed()),
			]);
		}
	}

	private sealed class Hook : IKokoroApi.IV2.ICardRenderingApi.IHook
	{
		public Font? ReplaceTextCardFont(IKokoroApi.IV2.ICardRenderingApi.IHook.IReplaceTextCardFontArgs args)
			=> args.Card is MissileSalvoCard ? ModEntry.Instance.KokoroApi.Assets.PinchCompactFont : null;
	}
}