using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class EnshroudCard : Card, IDraculaCard
{
	public static void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Enshroud", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Enshroud", "name"]).Localize
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (State s, Combat c) =>
		{
			if (!c.isPlayerTurn)
				return;

			List<Ship> ships = [s.ship, c.otherShip];
			foreach (var ship in ships)
			{
				foreach (var part in ((IEnumerable<Part>)ship.parts).Reverse())
				{
					if (!ModEntry.Instance.KokoroApi.TryGetExtensionData(part, "DamageModifierBeforeEnshroud", out PDamMod damageModifierBeforeEnshroud))
						continue;
					ModEntry.Instance.KokoroApi.RemoveExtensionData(part, "DamageModifierBeforeEnshroud");
					if (part.damageModifier == PDamMod.armor)
						part.damageModifier = damageModifierBeforeEnshroud;
				}
			}
		}, 0);
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade switch
			{
				Upgrade.A => 1,
				Upgrade.B => 0,
				_ => 2
			},
			description = ModEntry.Instance.Localizations.Localize(["card", "Enshroud", "description", upgrade.ToString()])
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<Ship> ships = [s.ship];
		if (upgrade == Upgrade.B)
			ships.Add(c.otherShip);

		List<CardAction> actions = [];
		foreach (var ship in ships)
			for (var partIndex = 0; partIndex < ship.parts.Count; partIndex++)
				if (ship.parts[partIndex].type != PType.empty)
					actions.Add(new AEnshroudPart
					{
						TargetPlayer = ship.isPlayerShip,
						WorldX = ship.x + partIndex,
						omitFromTooltips = true,
					});
		return actions;
	}

	public sealed class AEnshroudPart : CardAction
	{
		[JsonProperty]
		public required bool TargetPlayer;

		[JsonProperty]
		public required int WorldX;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var ship = TargetPlayer ? s.ship : c.otherShip;
			if (ship.GetPartAtWorldX(WorldX) is not { } part || part.damageModifier == PDamMod.armor)
				return;

			ModEntry.Instance.KokoroApi.SetExtensionData(part, "DamageModifierBeforeEnshroud", part.damageModifier);
			c.QueueImmediate(new AArmor
			{
				targetPlayer = TargetPlayer,
				worldX = WorldX
			});
		}
	}
}
