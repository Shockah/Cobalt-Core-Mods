using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal static class EnshroudExt
{
	public static PDamMod? GetDamageModifierBeforeEnshroud(this Part self)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<PDamMod>(self, "DamageModifierBeforeEnshroud");

	public static void SetDamageModifierBeforeEnshroud(this Part self, PDamMod? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "DamageModifierBeforeEnshroud", value);

	public static PDamMod? GetDamageModifierOverrideWhileActiveBeforeEnshroud(this Part self)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<PDamMod>(self, "DamageModifierOverrideWhileActiveBeforeEnshroud");

	public static void SetDamageModifierOverrideWhileActiveBeforeEnshroud(this Part self, PDamMod? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "DamageModifierOverrideWhileActiveBeforeEnshroud", value);
}

internal sealed class EnshroudCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
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
			Art = StableSpr.cards_CloudSave,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Enshroud", "name"]).Localize
		});

		helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (State state, Combat combat) =>
		{
			if (!combat.isPlayerTurn)
				return;

			List<Ship> ships = [state.ship, combat.otherShip];
			foreach (var ship in ships)
			{
				foreach (var part in ship.parts)
				{
					if (part.damageModifier == PDamMod.armor && part.GetDamageModifierBeforeEnshroud() is { } damageModifierBeforeEnshroud)
					{
						part.damageModifier = damageModifierBeforeEnshroud;
						part.SetDamageModifierBeforeEnshroud(null);
					}
					if (part.damageModifierOverrideWhileActive == PDamMod.armor && part.GetDamageModifierOverrideWhileActiveBeforeEnshroud() is { } damageModifierOverrideWhileActiveBeforeEnshroud)
					{
						part.damageModifierOverrideWhileActive = damageModifierOverrideWhileActiveBeforeEnshroud;
						part.SetDamageModifierOverrideWhileActiveBeforeEnshroud(null);
					}
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

		actions.Add(new ATooltipAction { Tooltips = [new TTGlossary("parttrait.armor")] });
		return actions;
	}

	public sealed class AEnshroudPart : CardAction
	{
		[JsonProperty]
		public required bool TargetPlayer;

		[JsonProperty]
		public required int WorldX;

		public override List<Tooltip> GetTooltips(State s)
			=> [new TTGlossary("parttrait.armor")];

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			var ship = TargetPlayer ? s.ship : c.otherShip;
			if (ship.GetPartAtWorldX(WorldX) is not { } part)
				return;

			if (part.damageModifier != PDamMod.armor)
			{
				part.SetDamageModifierBeforeEnshroud(part.damageModifier);
				c.QueueImmediate(new AArmor
				{
					targetPlayer = TargetPlayer,
					worldX = WorldX
				});
			}
			if (part.damageModifierOverrideWhileActive is not null && part.damageModifierOverrideWhileActive != PDamMod.armor)
			{
				part.SetDamageModifierOverrideWhileActiveBeforeEnshroud(part.damageModifierOverrideWhileActive);
				c.QueueImmediate(new AArmor
				{
					targetPlayer = TargetPlayer,
					worldX = WorldX,
					justTheActiveOverride = true
				});
			}
		}
	}
}
